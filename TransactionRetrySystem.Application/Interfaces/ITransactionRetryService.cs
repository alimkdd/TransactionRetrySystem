namespace TransactionRetrySystem.Application.Interfaces;

public interface ITransactionRetryService
{
    Task ProcessRetry(int transactionId, int attemptNumber, CancellationToken cancellationToken = default);
}
