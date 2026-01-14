using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Application.Transfers;

public record CreateTransferResponse
{
    public Guid TransferId { get; init; }
    public string TransactionCode { get; init; } = string.Empty;
    public TransferStatus Status { get; init; }
    public decimal Amount { get; init; }
    public Currency Currency { get; init; }
    public decimal AmountInTRY { get; init; }
    public decimal TransactionFee { get; init; }
    public RiskLevel? RiskLevel { get; init; }
    public DateTime? ApprovalRequiredUntil { get; init; }
    public string Message { get; init; } = string.Empty;

    public decimal TotalAmount => Amount + TransactionFee;
}
