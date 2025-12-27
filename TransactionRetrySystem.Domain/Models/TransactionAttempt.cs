namespace TransactionRetrySystem.Domain.Models;

public class TransactionAttempt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int StatusId { get; set; }
    public int ErrorTypeId { get; set; }
    public int AttemptNumber { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime AttemptedAt { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public string GatewayResponse { get; set; }
    public DateTime CreatedAt { get; set; }

    public ErrorType ErrorType { get; set; }
    public Status Status { get; set; }
}