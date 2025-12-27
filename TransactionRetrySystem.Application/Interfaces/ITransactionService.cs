using TransactionRetrySystem.Application.Dtos.Responses;

namespace TransactionRetrySystem.Application.Interfaces;

public interface ITransactionService
{
    Task<List<RetryHistoryResponse>> GetRetryHistory(int id, CancellationToken cancellationToken);

    Task<string> GetTransactionStatus(int id, CancellationToken cancellationToken);

    Task<string> RetryTransaction(int id, CancellationToken cancellationToken);

    Task<string> CancelTransactionRetries(int id, CancellationToken cancellationToken);
}