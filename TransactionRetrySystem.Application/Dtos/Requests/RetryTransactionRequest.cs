namespace TransactionRetrySystem.Application.Dtos.Requests;

public record RetryTransactionRequest(
    int TransactionId,
    int AttemptNumber
    );