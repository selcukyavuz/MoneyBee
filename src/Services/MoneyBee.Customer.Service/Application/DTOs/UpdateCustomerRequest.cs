namespace MoneyBee.Customer.Service.Application.DTOs;

public record UpdateCustomerRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? Email { get; init; }
}
