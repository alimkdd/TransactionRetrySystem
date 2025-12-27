namespace TransactionRetrySystem.Application.Dtos.Responses;

public record RetryHistoryResponse(
    int TransactionId,
    string Status,
    DateTime ScheduledRetryTime,
    int RetryCount);
