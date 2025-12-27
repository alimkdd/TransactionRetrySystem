using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using System.Net;
using TransactionRetrySystem.Application.Dtos.Configs;
using TransactionRetrySystem.Application.Dtos.Requests;
using TransactionRetrySystem.Application.Dtos.Responses;
using TransactionRetrySystem.Application.Interfaces;
using TransactionRetrySystem.Domain.Enums;
using TransactionRetrySystem.Domain.Models;
using TransactionRetrySystem.Infrastructure.Context;
using ErrorType = TransactionRetrySystem.Domain.Enums.ErrorType;

namespace TransactionRetrySystem.Application.Services;

public class TransactionRetryService : ITransactionRetryService
{
    private readonly AppDbContext _dbContext;
    private readonly IErrorClassifier _errorClassifier;
    private readonly IUserRateLimiter _rateLimiter;
    private readonly IGatewayCircuitBreaker _circuitBreaker;
    private readonly IMessageScheduler _scheduler;
    private readonly RetryConfiguration _retryConfig;
    private readonly Random _random = new();

    public TransactionRetryService(
        AppDbContext dbContext,
        IErrorClassifier errorClassifier,
        IUserRateLimiter rateLimiter,
        IGatewayCircuitBreaker circuitBreaker,
        IMessageScheduler scheduler,
        IOptions<RetryConfiguration> retryConfigOptions)
    {
        _dbContext = dbContext;
        _errorClassifier = errorClassifier;
        _rateLimiter = rateLimiter;
        _circuitBreaker = circuitBreaker;
        _scheduler = scheduler;
        _retryConfig = retryConfigOptions.Value;
    }

    public async Task ProcessRetry(int transactionId, int attemptNumber, CancellationToken cancellationToken = default)
    {
        // Fetch the most recent attempt for the transaction
        var transaction = await _dbContext.TransactionAttempts
                                .Where(t => t.Id == transactionId)
                                .OrderByDescending(t => t.AttemptedAt)
                                .FirstOrDefaultAsync(cancellationToken);

        // Exit early if transaction doesn't exist or has already succeeded
        if (transaction == null || transaction.StatusId == (int)TransactionStatus.Succeeded)
            return;

        // Idempotency check: skip if already processing or retrying
        if (transaction.StatusId == (int)TransactionStatus.Processing || transaction.StatusId == (int)TransactionStatus.Retrying)
            return;

        // Check if the user has exceeded failure threshold in the last 1 hour
        var failedCount = await _dbContext.TransactionAttempts
            .Where(t => t.UserId == transaction.UserId &&
                        t.StatusId == (int)TransactionStatus.Failed &&
                        t.AttemptedAt >= DateTime.UtcNow.AddHours(-1))
            .CountAsync(cancellationToken);

        if (failedCount > 5)
        {
            // Mark transaction as failed and require manual intervention
            transaction.StatusId = (int)TransactionStatus.Failed;
            transaction.ErrorMessage = "Requires manual verification due to repeated failures";
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // Mark transaction as currently processing
        transaction.StatusId = (int)TransactionStatus.Processing;
        transaction.AttemptedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            // Get the circuit breaker policy for the transaction's gateway
            var circuitPolicy = await _circuitBreaker.GetPolicy(transaction.GatewayResponse ?? "DefaultGateway");

            // Execute the transaction call under the circuit breaker
            await circuitPolicy.ExecuteAsync(async () =>
            {
                // Simulate calling the payment gateway
                var gatewayResponse = await CallPaymentGateway(transaction.ErrorTypeId);

                // Classify the error returned from the gateway
                var errorType = _errorClassifier.ClassifyError(gatewayResponse);

                // Check if the error is retryable
                if (_errorClassifier.IsRetryable(errorType))
                {
                    // Retrieve retry policy for the error type
                    var strategy = GetRetryPolicy(errorType);

                    // Adjust retry strategy for peak traffic hours
                    AdjustAttemptsForHighTraffic(ref strategy);

                    // If max attempts not yet reached, schedule retry
                    if (attemptNumber <= strategy.MaxAttempts)
                    {
                        var nextAttempt = attemptNumber + 1;

                        // Calculate delay using exponential backoff + jitter
                        var delay = GetRetryDelay(strategy, attemptNumber);

                        // Record the retry in RetryQueue table
                        _dbContext.RetryQueue.Add(new RetryQueue
                        {
                            TransactionId = transactionId,
                            RetryCount = nextAttempt,
                            StatusId = (int)TransactionStatus.Retrying,
                            ScheduledRetryTime = DateTime.UtcNow.Add(delay),
                            CreatedAt = DateTime.UtcNow
                        });
                        await _dbContext.SaveChangesAsync();

                        // Prepare a retry message for the queue
                        var retryTransactionRequest = new RetryTransactionRequest(transactionId, nextAttempt);

                        // Schedule message for future delivery
                        await _scheduler.ScheduleSend(
                            new Uri("queue:transaction-retry-queue"),
                            DateTime.UtcNow + delay,
                            retryTransactionRequest,
                            cancellationToken
                        );

                        // Update transaction as retrying
                        transaction.StatusId = (int)TransactionStatus.Retrying;
                        transaction.AttemptedAt = DateTime.UtcNow;
                        transaction.ErrorTypeId = (int)errorType;
                        transaction.ErrorMessage = gatewayResponse.ErrorMessage;

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        return;
                    }
                }

                // Handle success or permanent failure
                transaction.StatusId = gatewayResponse.Success ? (int)TransactionStatus.Succeeded : (int)TransactionStatus.Failed;
                transaction.ErrorTypeId = 0;
                transaction.ErrorMessage = gatewayResponse.ErrorMessage;
                transaction.AttemptedAt = DateTime.UtcNow;

                // Record failures in rate limiter and circuit breaker if transaction failed
                if (!gatewayResponse.Success)
                {
                    await _rateLimiter.IncrementFailure(transaction.Id);
                    await _circuitBreaker.RecordFailure(transaction.GatewayResponse ?? "DefaultGateway");
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Special edge case for delayed success on network timeouts
                if (errorType == ErrorType.NetworkTimeout)
                {
                    bool alreadySucceeded = await CheckTransactionStatusBeforeRetry(transactionId);
                    if (alreadySucceeded)
                    {
                        transaction.StatusId = (int)TransactionStatus.Succeeded;
                        transaction.ErrorTypeId = 0;
                        transaction.ErrorMessage = null;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        return;
                    }
                }
            });
        }
        catch (BrokenCircuitException)
        {
            // Circuit breaker is open; schedule retry after break duration
            var retryMessage = new RetryTransactionRequest(transactionId, attemptNumber);

            await _scheduler.ScheduleSend(
                new Uri("queue:transaction-retry-queue"),
                DateTime.UtcNow + _circuitBreaker.GetConfig().ResetDuration,
                retryMessage,
                cancellationToken
            );

            transaction.StatusId = (int)TransactionStatus.Retrying;
            transaction.AttemptedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Handle unexpected exceptions
            transaction.StatusId = (int)TransactionStatus.Failed;
            transaction.ErrorTypeId = (int)ErrorType.Unknown;
            transaction.ErrorMessage = ex.Message;
            transaction.AttemptedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private void AdjustAttemptsForHighTraffic(ref RetryStrategyRequest strategy)
    {
        var now = DateTime.UtcNow.TimeOfDay;
        if ((now >= TimeSpan.FromHours(12) && now < TimeSpan.FromHours(14)) ||
            (now >= TimeSpan.FromHours(18) && now < TimeSpan.FromHours(20)))
        {
            strategy = strategy with
            {
                MaxAttempts = Math.Max(1, strategy.MaxAttempts - 1)
            };
        }
    }

    private RetryStrategyRequest GetRetryPolicy(ErrorType errorType)
    {
        RetryPolicy policy = errorType switch
        {
            ErrorType.NetworkTimeout => _retryConfig.NetworkTimeout,
            ErrorType.GatewayBusy => _retryConfig.GatewayBusy,
            ErrorType.RateLimitExceeded => _retryConfig.RateLimitExceeded,
            ErrorType.TemporaryServerError => _retryConfig.TemporaryServerError,
            _ => null
        };

        if (policy == null)
            return new RetryStrategyRequest(0, new List<TimeSpan>(), false);

        return new RetryStrategyRequest(
            policy.MaxAttempts,
            policy.DelaysInSeconds.Select(s => TimeSpan.FromSeconds(s)).ToList(),
            policy.UseExponentialBackoff
        );
    }

    private TimeSpan GetRetryDelay(RetryStrategyRequest strategy, int attemptNumber)
    {
        if (!strategy.UseExponentialBackoff) return strategy.RetryDelays[attemptNumber];

        var baseDelay = strategy.RetryDelays[attemptNumber];
        var jitter = TimeSpan.FromMilliseconds(_random.Next(0, 1000)); // random offset
        var exponential = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attemptNumber));
        return exponential + jitter;
    }

    private async Task<PaymentGatewayResponse> CallPaymentGateway(int errorTypeId)
    {
        // Simulate delay
        await Task.Delay(50);

        // Map ErrorTypeId to gateway response
        return errorTypeId switch
        {
            1 => new PaymentGatewayResponse(false, "NETWORK_TIMEOUT", HttpStatusCode.RequestTimeout, "Simulated network timeout"),
            2 => new PaymentGatewayResponse(false, "GATEWAY_BUSY", HttpStatusCode.ServiceUnavailable, "Simulated gateway busy"),
            3 => new PaymentGatewayResponse(false, "RATE_LIMIT_EXCEEDED", HttpStatusCode.TooManyRequests, "Simulated rate limit exceeded"),
            4 => new PaymentGatewayResponse(false, "TEMPORARY_SERVER_ERROR", HttpStatusCode.InternalServerError, "Simulated temporary server error"),
            5 => new PaymentGatewayResponse(false, "CARD_DECLINED", HttpStatusCode.PaymentRequired, "Simulated card declined"),
            _ => new PaymentGatewayResponse(false, "UNKNOWN", HttpStatusCode.InternalServerError, "Simulated unknown error"),
        };
    }

    private async Task<bool> CheckTransactionStatusBeforeRetry(int transactionId)
    {
        await Task.Delay(50);

        var response = new PaymentGatewayResponse(
            Success: true,
            ErrorCode: null,
            ErrorMessage: null,
            StatusCode: HttpStatusCode.OK
        );

        return response.Success;
    }
}