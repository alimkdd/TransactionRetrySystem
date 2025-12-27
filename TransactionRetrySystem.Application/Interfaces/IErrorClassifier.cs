using TransactionRetrySystem.Application.Dtos.Responses;
using TransactionRetrySystem.Domain.Enums;

namespace TransactionRetrySystem.Application.Interfaces;

public interface IErrorClassifier
{
    ErrorType ClassifyError(PaymentGatewayResponse response);

    bool IsRetryable(ErrorType errorType);
}