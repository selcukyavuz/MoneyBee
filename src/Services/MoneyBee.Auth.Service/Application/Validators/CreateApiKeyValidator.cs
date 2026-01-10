using FluentValidation;
using MoneyBee.Auth.Service.Application.DTOs;

namespace MoneyBee.Auth.Service.Application.Validators;

public class CreateApiKeyValidator : AbstractValidator<CreateApiKeyRequest>
{
    public CreateApiKeyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ExpiresInDays)
            .GreaterThan(0).WithMessage("ExpiresInDays must be greater than 0")
            .LessThanOrEqualTo(3650).WithMessage("ExpiresInDays cannot exceed 3650 (10 years)")
            .When(x => x.ExpiresInDays.HasValue);
    }
}
