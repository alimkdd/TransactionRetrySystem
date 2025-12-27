namespace TransactionRetrySystem.Domain.Enums;

public enum ErrorType
{
    NetworkTimeout = 1,
    GatewayBusy = 2,
    RateLimitExceeded = 3,
    TemporaryServerError = 4,
    CardDeclined = 5,
    InsufficientFunds = 6,
    InvalidAccountNumber = 7,
    FraudDetected = 8,
    AuthenticationFailed = 9,
    Unknown = 10
}