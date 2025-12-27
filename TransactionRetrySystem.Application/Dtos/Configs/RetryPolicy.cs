namespace TransactionRetrySystem.Application.Dtos.Configs;

public class RetryPolicy
{
    public int MaxAttempts { get; set; }
    public List<int> DelaysInSeconds { get; set; } = new();
    public bool UseExponentialBackoff { get; set; } = true;

    public RetryPolicy() { }
}