using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Application.DTOs;

public class CreateTransferRequest
{
    public string SenderNationalId { get; set; } = string.Empty;
    public string ReceiverNationalId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Currency Currency { get; set; } = Currency.TRY;
    public string? IdempotencyKey { get; set; }
}

public class CreateTransferResponse
{
    public Guid TransferId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public TransferStatus Status { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public decimal AmountInTRY { get; set; }
    public decimal TransactionFee { get; set; }
    public decimal TotalAmount => Amount + TransactionFee;
    public RiskLevel? RiskLevel { get; set; }
    public DateTime? ApprovalRequiredUntil { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CompleteTransferRequest
{
    public string ReceiverNationalId { get; set; } = string.Empty;
}

public class TransferDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string? SenderNationalId { get; set; }
    public string? ReceiverNationalId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public decimal AmountInTRY { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal TransactionFee { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public TransferStatus Status { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? ApprovalRequiredUntil { get; set; }
    public bool CanBeCompleted => Status == TransferStatus.Pending && 
                                  (!ApprovalRequiredUntil.HasValue || ApprovalRequiredUntil.Value <= DateTime.UtcNow);
    public bool CanBeCancelled => Status == TransferStatus.Pending;
}

public class CancelTransferRequest
{
    public string Reason { get; set; } = "Customer request";
}

public class DailyLimitCheckResponse
{
    public decimal TotalTransfersToday { get; set; }
    public decimal DailyLimit { get; set; } = 10000;
    public decimal RemainingLimit => Math.Max(0, DailyLimit - TotalTransfersToday);
    public bool CanTransfer(decimal amount) => TotalTransfersToday + amount <= DailyLimit;
}
