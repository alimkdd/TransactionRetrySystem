namespace TransactionRetrySystem.Application.Dtos.Configs;

public class CircuitBreakerConfiguration
{
    public int ResetTimeout { get; set; }
    public int FailureThreshold { get; set; }
    public TimeSpan ResetDuration => TimeSpan.FromSeconds(ResetTimeout);
    public TimeSpan failureThreshold => TimeSpan.FromSeconds(FailureThreshold);
}