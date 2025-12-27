namespace TransactionRetrySystem.Domain.Enums;

public enum TransactionStatus
{
    Pending = 1,
    Processing = 2,
    Retrying = 3,
    Succeeded = 4,
    Failed = 5,
    Cancelled = 6
}