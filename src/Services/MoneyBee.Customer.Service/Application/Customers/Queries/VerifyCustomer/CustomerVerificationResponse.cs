using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Application.Customers.Queries.VerifyCustomer;

public record CustomerVerificationResponse
{
    public bool Exists { get; init; }
    public Guid? CustomerId { get; init; }
    public CustomerStatus? Status { get; init; }
    public bool? KycVerified { get; init; }
    public bool IsActive { get; init; }
    public string Message { get; init; } = string.Empty;
}
