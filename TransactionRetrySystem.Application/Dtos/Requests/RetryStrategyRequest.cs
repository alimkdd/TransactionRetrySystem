namespace TransactionRetrySystem.Application.Dtos.Requests;

public record RetryStrategyRequest(
    int MaxAttempts,
    List<TimeSpan> RetryDelays,
    bool UseExponentialBackoff = true
    );