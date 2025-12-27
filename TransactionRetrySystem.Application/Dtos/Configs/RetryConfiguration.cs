namespace TransactionRetrySystem.Application.Dtos.Configs;

public class RetryConfiguration
{
    public RetryPolicy NetworkTimeout { get; set; }
    public RetryPolicy GatewayBusy { get; set; }
    public RetryPolicy RateLimitExceeded { get; set; }
    public RetryPolicy TemporaryServerError { get; set; }

    // Parameterless constructor required for IOptions binding
    public RetryConfiguration() { }
}