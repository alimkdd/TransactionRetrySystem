namespace TransactionRetrySystem.Domain.Models;

public class ErrorType
{
    public int Id { get; set; }

    public string Name { get; set; }

    public DateTime CreatedAt { get; set; }
}