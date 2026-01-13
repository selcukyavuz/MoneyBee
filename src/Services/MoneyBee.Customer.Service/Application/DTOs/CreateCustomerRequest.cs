using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Application.DTOs;

public record CreateCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string NationalId { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public CustomerType CustomerType { get; init; }
    public string? TaxNumber { get; init; }
    public string? Address { get; init; }
    public string? Email { get; init; }
}
