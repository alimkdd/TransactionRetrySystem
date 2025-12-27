namespace TransactionRetrySystem.Domain.Models;

public class CircuitBreakerState
{
    public int Id { get; set; }
    public string Gateway { get; set; }
    public string State { get; set; }
    public int FailureCount { get; set; }
    public DateTime LastFailureTime { get; set; }
    public DateTime CreatedAt { get; set; }
}