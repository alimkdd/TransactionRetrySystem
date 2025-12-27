using FluentValidation;
using TransactionRetrySystem.Application.Dtos.Requests;

namespace TransactionRetrySystem.Application.Validators;

public class RetryTransactionRequestValidator : AbstractValidator<RetryTransactionRequest>
{
    public RetryTransactionRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0)
            .WithMessage("TransactionId must be greater than 0.");
    }
}