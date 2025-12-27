namespace TransactionRetrySystem.Application.Interfaces;

public interface IUserRateLimiter
{
    Task IncrementFailure(int userId);

    Task<int> GetFailures(int userId);
}