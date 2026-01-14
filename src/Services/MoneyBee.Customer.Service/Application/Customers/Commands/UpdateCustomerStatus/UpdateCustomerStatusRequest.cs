using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Application.Customers.Commands.UpdateCustomerStatus;

public record UpdateCustomerStatusRequest
{
    public Guid Id { get; init; }
    public CustomerStatus Status { get; init; }
    public string Reason { get; init; } = string.Empty;
}
