namespace MoneyBee.Customer.Service.Application.DTOs;

public record CustomerVerificationRequest
{
    public string NationalId { get; init; } = string.Empty;
}
