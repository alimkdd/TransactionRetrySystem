namespace TransactionRetrySystem.Application.Dtos.Responses;

public record TransactionStatusResponse(
    int TransactionId,
    int StatusId,
    int ErrorTypeId,
    string ErrorMessage,
    DateTime AttemptedAt
    );