using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Application.DTOs;

public record CreateTransferRequest
{
    public string SenderNationalId { get; init; } = string.Empty;
    public string ReceiverNationalId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public Currency Currency { get; init; } = Currency.TRY;
    public string? IdempotencyKey { get; init; }
}
