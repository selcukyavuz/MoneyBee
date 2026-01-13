using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.CustomerService;

/// <summary>
/// Represents basic customer information retrieved from Customer Service
/// </summary>
public record CustomerInfo
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string NationalId { get; init; }
    public required CustomerStatus Status { get; init; }
    public required bool KycVerified { get; init; }
}
