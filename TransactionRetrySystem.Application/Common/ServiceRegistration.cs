using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using TransactionRetrySystem.Application.Dtos.Configs;
using TransactionRetrySystem.Application.Dtos.Requests;
using TransactionRetrySystem.Application.Interfaces;
using TransactionRetrySystem.Application.Services;
using TransactionRetrySystem.Application.Services.CircuitBreaker;
using TransactionRetrySystem.Application.Services.RateLimiter;
using TransactionRetrySystem.Application.Validators;

namespace TransactionRetrySystem.Application.Common;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
    {
        // Error Classification Service
        services.AddSingleton<IErrorClassifier, ErrorClassifier>();

        // Transaction Services
        services.AddScoped<ITransactionService, TransactionService>();

        // Transaction Retry Service
        services.AddScoped<ITransactionRetryService, TransactionRetryService>();

        // Circuit Breaker
        services.AddScoped<IGatewayCircuitBreaker, GatewayCircuitBreaker>();

        // Redis Rate Limiter
        services.AddScoped<IUserRateLimiter, UserRateLimiter>();

        // Redis Connection
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            return ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis"));
        });

        // Configuration Binding
        services.Configure<RetryConfiguration>(configuration.GetSection("RetryConfiguration"));
        services.Configure<CircuitBreakerConfiguration>(configuration.GetSection("CircuitBreaker"));

        // Fluent Validation
        services.AddValidatorsFromAssemblyContaining<RetryStrategyRequestValidator>();
        services.AddValidatorsFromAssemblyContaining<RetryTransactionRequestValidator>();
        services.AddFluentValidationAutoValidation();

        return services;
    }
}