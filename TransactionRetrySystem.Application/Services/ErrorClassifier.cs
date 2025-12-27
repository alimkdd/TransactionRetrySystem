using System.Net;
using TransactionRetrySystem.Application.Dtos.Responses;
using TransactionRetrySystem.Application.Interfaces;
using TransactionRetrySystem.Domain.Enums;

namespace TransactionRetrySystem.Application.Services;

public class ErrorClassifier : IErrorClassifier
{
    public ErrorType ClassifyError(PaymentGatewayResponse response)
    {
        if (response.Success)
            return ErrorType.Unknown;

        return response.StatusCode switch
        {
            HttpStatusCode.RequestTimeout => ErrorType.NetworkTimeout,
            HttpStatusCode.ServiceUnavailable => ErrorType.GatewayBusy,
            HttpStatusCode.TooManyRequests => ErrorType.RateLimitExceeded,
            HttpStatusCode.InternalServerError => ErrorType.TemporaryServerError,
            HttpStatusCode.Unauthorized => ErrorType.AuthenticationFailed,
            _ => response.ErrorCode switch
            {
                "NETWORK_TIMEOUT" => ErrorType.NetworkTimeout,
                "CARD_DECLINED" => ErrorType.CardDeclined,
                "INSUFFICIENT_FUNDS" => ErrorType.InsufficientFunds,
                "INVALID_ACCOUNT" => ErrorType.InvalidAccountNumber,
                "FRAUD_DETECTED" => ErrorType.FraudDetected,
                _ => ErrorType.Unknown
            }
        };
    }

    public bool IsRetryable(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.NetworkTimeout => true,
            ErrorType.GatewayBusy => true,
            ErrorType.RateLimitExceeded => true,
            ErrorType.TemporaryServerError => true,
            _ => false
        };
    }
}