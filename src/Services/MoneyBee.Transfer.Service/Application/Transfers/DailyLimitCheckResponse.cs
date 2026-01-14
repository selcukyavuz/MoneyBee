namespace MoneyBee.Transfer.Service.Application.Transfers;

public record DailyLimitCheckResponse
{
    public decimal TotalTransfersToday { get; init; }
    public decimal DailyLimit { get; init; } = 10000;

    public decimal RemainingLimit => Math.Max(0, DailyLimit - TotalTransfersToday);
    public bool CanTransfer(decimal amount) => TotalTransfersToday + amount <= DailyLimit;
}
