namespace MoneyBee.Customer.Service.Application.Customers;

public record CustomerVerificationRequest
{
    public string NationalId { get; init; } = string.Empty;
}
