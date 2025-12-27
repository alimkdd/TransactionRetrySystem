using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using System.Collections.Concurrent;
using TransactionRetrySystem.Application.Dtos.Configs;
using TransactionRetrySystem.Application.Interfaces;
using TransactionRetrySystem.Domain.Models;
using TransactionRetrySystem.Infrastructure.Context;

namespace TransactionRetrySystem.Application.Services.CircuitBreaker;

public class GatewayCircuitBreaker : IGatewayCircuitBreaker
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly CircuitBreakerConfiguration _config;
    private readonly ConcurrentDictionary<string, AsyncPolicy> _policies = new();

    public CircuitBreakerConfiguration GetConfig() => _config;

    public GatewayCircuitBreaker(IDbContextFactory<AppDbContext> dbContextFactory, IOptions<CircuitBreakerConfiguration> configOptions)
    {
        _dbContextFactory = dbContextFactory;
        _config = configOptions.Value;
    }

    public async Task<AsyncPolicy> GetPolicy(string gateway)
    {
        return _policies.GetOrAdd(gateway, _ =>
            Policy.Handle<Exception>()
                  .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: _config.FailureThreshold,
                    durationOfBreak: _config.ResetDuration,
                    onBreak: async (ex, breakDelay) => await PersistCircuitState(gateway, "Open"),
                    onReset: async () => await PersistCircuitState(gateway, "Closed"),
                    onHalfOpen: async () => await PersistCircuitState(gateway, "HalfOpen")
                )
        );
    }

    public Task RecordFailure(string gateway)
    {
        Console.WriteLine($"[{gateway}] Failure recorded for monitoring.");
        return Task.CompletedTask;
    }

    private async Task PersistCircuitState(string gateway, string state)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var record = await dbContext.CircuitBreakerStates.FirstOrDefaultAsync(c => c.Gateway == gateway);

        if (record == null)
        {
            record = new CircuitBreakerState
            {
                Gateway = gateway,
                State = state,
                FailureCount = 1,
                LastFailureTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            dbContext.CircuitBreakerStates.Add(record);
        }
        else
        {
            record.State = state;
            record.FailureCount += 1;
            record.LastFailureTime = DateTime.UtcNow;
            record.CreatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }
}