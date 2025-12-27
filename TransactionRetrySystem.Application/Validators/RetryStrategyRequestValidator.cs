using FluentValidation;
using TransactionRetrySystem.Application.Dtos.Requests;

namespace TransactionRetrySystem.Application.Validators;

public class RetryStrategyRequestValidator : AbstractValidator<RetryStrategyRequest>
{
    public RetryStrategyRequestValidator()
    {
        RuleFor(x => x.MaxAttempts)
            .GreaterThan(0)
            .WithMessage("MaxAttempts must be greater than 0.");

        RuleFor(x => x.RetryDelays)
            .NotNull()
            .WithMessage("RetryDelays cannot be null.")
            .Must(list => list.Any())
            .WithMessage("RetryDelays must contain at least one delay.")
            .Must(list => list.All(d => d.TotalMilliseconds > 0))
            .WithMessage("All RetryDelays must be greater than zero.");
    }
}