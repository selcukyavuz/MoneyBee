using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Application.DTOs;

public record UpdateCustomerStatusRequest
{
    public CustomerStatus Status { get; init; }
    public string Reason { get; init; } = string.Empty;
}
