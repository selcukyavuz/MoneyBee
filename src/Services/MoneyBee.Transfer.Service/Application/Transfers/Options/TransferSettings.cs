namespace MoneyBee.Transfer.Service.Application.Transfers.Options;

/// <summary>
/// Transfer business logic settings
/// </summary>
public class TransferSettings
{
    public decimal DailyLimitTRY { get; set; }
    public decimal HighAmountThresholdTRY { get; set; }
    public int ApprovalWaitMinutes { get; set; }
    public int DailyLimitCheckTimeoutSeconds { get; set; }
}
