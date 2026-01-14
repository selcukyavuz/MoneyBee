using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Application.Transfers.Shared;

public record TransferDto
{
    public Guid Id { get; init; }
    public Guid SenderId { get; init; }
    public Guid ReceiverId { get; init; }
    public string? SenderNationalId { get; init; }
    public string? ReceiverNationalId { get; init; }
    public decimal Amount { get; init; }
    public Currency Currency { get; init; }
    public decimal AmountInTRY { get; init; }
    public decimal? ExchangeRate { get; init; }
    public decimal TransactionFee { get; init; }
    public string TransactionCode { get; init; } = string.Empty;
    public TransferStatus Status { get; init; }
    public RiskLevel? RiskLevel { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public DateTime? ApprovalRequiredUntil { get; init; }

    public bool CanBeCompleted => Status == TransferStatus.Pending && 
                                  (!ApprovalRequiredUntil.HasValue || ApprovalRequiredUntil.Value <= DateTime.UtcNow);
    public bool CanBeCancelled => Status == TransferStatus.Pending;
}
