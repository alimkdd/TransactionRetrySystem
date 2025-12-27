namespace TransactionRetrySystem.Domain.Models;

public class RetryQueue
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int StatusId { get; set; }
    public DateTime ScheduledRetryTime { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public Status Status { get; set; }
}