using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Application.Transfers.Shared;

/// <summary>
/// Represents an exchange rate between two currencies
/// </summary>
public record ExchangeRateResult
{
    public required decimal Rate { get; init; }
    public required Currency FromCurrency { get; init; }
    public required Currency ToCurrency { get; init; }
    public required DateTime Timestamp { get; init; }
}
