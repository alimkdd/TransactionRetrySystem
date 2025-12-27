using MassTransit;
using TransactionRetrySystem.Application.Dtos.Requests;
using TransactionRetrySystem.Application.Interfaces;

namespace TransactionRetrySystem.Application.Services.TransactionConsumer;

public class RetryTransactionConsumer : IConsumer<RetryTransactionRequest>
{
    private readonly ITransactionRetryService _transactionRetryService;

    public RetryTransactionConsumer(ITransactionRetryService transactionRetryService)
    {
        _transactionRetryService = transactionRetryService;
    }

    public async Task Consume(ConsumeContext<RetryTransactionRequest> context)
    {
        var message = context.Message;
        await _transactionRetryService.ProcessRetry(message.TransactionId, message.AttemptNumber, context.CancellationToken);
    }
}