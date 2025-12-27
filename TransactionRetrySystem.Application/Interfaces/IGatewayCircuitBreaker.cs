using Polly;
using TransactionRetrySystem.Application.Dtos.Configs;

namespace TransactionRetrySystem.Application.Interfaces;

public interface IGatewayCircuitBreaker
{
    CircuitBreakerConfiguration GetConfig();

    public Task<AsyncPolicy> GetPolicy(string gateway);

    Task RecordFailure(string gateway);
}