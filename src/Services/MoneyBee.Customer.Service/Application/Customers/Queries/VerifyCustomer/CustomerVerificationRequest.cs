namespace MoneyBee.Customer.Service.Application.Customers.Queries.VerifyCustomer;

public record CustomerVerificationRequest
{
    public string NationalId { get; init; } = string.Empty;
}
