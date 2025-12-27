using Mapster;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using TransactionRetrySystem.Application.Dtos.Requests;
using TransactionRetrySystem.Application.Dtos.Responses;
using TransactionRetrySystem.Application.Interfaces;
using TransactionRetrySystem.Domain.Enums;
using TransactionRetrySystem.Infrastructure.Context;

namespace TransactionRetrySystem.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public TransactionService(AppDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<List<RetryHistoryResponse>> GetRetryHistory(int transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _dbContext.RetryQueue
                                .Include(t => t.Status)
                                .AsNoTracking()
                                .Where(t => t.TransactionId == transactionId)
                                .OrderBy(t => t.RetryCount)
                                .ToListAsync(cancellationToken);

            if (history == null)
                throw new KeyNotFoundException("History not found.");

            return history.Adapt<List<RetryHistoryResponse>>();
        }
        catch (Exception e)
        {
            throw e.InnerException;
        }
    }

    public async Task<string> GetTransactionStatus(int transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = await _dbContext.TransactionAttempts
                                    .Include(t => t.Status)
                                    .AsNoTracking()
                                    .Where(t => t.Id == transactionId)
                                    .OrderByDescending(t => t.AttemptedAt)
                                    .FirstOrDefaultAsync(cancellationToken);

            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found.");

            return transaction.Status.Name;
        }
        catch (Exception e)
        {
            throw e.InnerException;
        }
    }

    public async Task<string> RetryTransaction(int transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = await _dbContext.TransactionAttempts
                                    .Where(t => t.Id == transactionId)
                                    .OrderByDescending(t => t.AttemptedAt)
                                    .FirstOrDefaultAsync(cancellationToken);

            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found.");

            var message = new RetryTransactionRequest(transactionId, transaction.AttemptNumber);

            await _publishEndpoint.Publish(message, cancellationToken);

            return "Retry Scheduled!";
        }
        catch (Exception e)
        {
            throw e.InnerException;
        }
    }

    public async Task<string> CancelTransactionRetries(int transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingRetries = await _dbContext.RetryQueue
                                       .Where(r => r.TransactionId == transactionId &&
                                              r.StatusId == (int)TransactionStatus.Retrying)
                                       .ToListAsync(cancellationToken);

            if (!pendingRetries.Any())
                throw new KeyNotFoundException("No pending retries found.");

            foreach (var retry in pendingRetries)
                retry.StatusId = (int)TransactionStatus.Cancelled;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return "Pending Retries Cancelled!";
        }
        catch (Exception e)
        {
            throw e.InnerException;
        }
    }
}