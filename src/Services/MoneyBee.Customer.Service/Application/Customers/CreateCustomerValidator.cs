using FluentValidation;
using MoneyBee.Customer.Service.Application.Customers.Commands.CreateCustomer;
using MoneyBee.Customer.Service.Domain.Customers;

namespace MoneyBee.Customer.Service.Application.Customers;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID is required")
            .Must(nationalId => NationalIdValidator.IsValid(NationalIdValidator.Normalize(nationalId)))
            .WithMessage("Invalid National ID format");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .Must(dob =>
            {
                var age = DateTime.Today.Year - dob.Year;
                if (dob.Date > DateTime.Today.AddYears(-age)) age--;
                return age >= 18;
            }).WithMessage("Customer must be at least 18 years old");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Tax number is required for corporate customers")
            .When(x => x.CustomerType == Common.Enums.CustomerType.Corporate);
    }
}
