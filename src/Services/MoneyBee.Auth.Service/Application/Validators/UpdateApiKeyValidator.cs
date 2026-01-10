using FluentValidation;
using MoneyBee.Auth.Service.Application.DTOs;

namespace MoneyBee.Auth.Service.Application.Validators;

public class UpdateApiKeyValidator : AbstractValidator<UpdateApiKeyRequest>
{
    public UpdateApiKeyValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
