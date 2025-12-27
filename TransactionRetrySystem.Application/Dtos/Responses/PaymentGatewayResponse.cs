using System.Net;

namespace TransactionRetrySystem.Application.Dtos.Responses;

public record PaymentGatewayResponse(
     bool Success,
     string ErrorCode,
     HttpStatusCode StatusCode,
     string ErrorMessage
);