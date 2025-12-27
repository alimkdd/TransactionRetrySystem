using StackExchange.Redis;
using TransactionRetrySystem.Application.Interfaces;

namespace TransactionRetrySystem.Application.Services.RateLimiter;

public class UserRateLimiter : IUserRateLimiter
{
    private readonly IConnectionMultiplexer _redis;

    public UserRateLimiter(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task IncrementFailure(int userId)
    {
        var key = $"user:{userId}:failures";
        await Db.StringIncrementAsync(key);
        await Db.KeyExpireAsync(key, TimeSpan.FromHours(1));
    }

    public async Task<int> GetFailures(int userId)
    {
        var key = $"user:{userId}:failures";
        var count = await Db.StringGetAsync(key);
        return (int)(count.IsNull ? 0 : count);
    }

    private IDatabase Db => _redis.GetDatabase();
}