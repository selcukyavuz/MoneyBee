using System.ComponentModel.DataAnnotations;
using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Domain.Entities;

/// <summary>
/// Represents a money transfer transaction with fraud detection and business rules
/// </summary>
public class Transfer
{
    /// <summary>
    /// Gets the unique identifier of the transfer
    /// </summary>
    [Key]
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the sender customer's unique identifier
    /// </summary>
    [Required]
    public Guid SenderId { get; private set; }

    /// <summary>
    /// Gets the receiver customer's unique identifier
    /// </summary>
    [Required]
    public Guid ReceiverId { get; private set; }

    /// <summary>
    /// Gets the transfer amount in original currency
    /// </summary>
    [Required]
    public decimal Amount { get; private set; }

    /// <summary>
    /// Gets the currency of the transfer
    /// </summary>
    [Required]
    public Currency Currency { get; private set; }

    /// <summary>
    /// Gets the transfer amount converted to TRY for limit checks
    /// </summary>
    [Required]
    public decimal AmountInTRY { get; private set; }

    /// <summary>
    /// Gets the exchange rate used for currency conversion
    /// </summary>
    public decimal? ExchangeRate { get; private set; }

    /// <summary>
    /// Gets the transaction fee charged
    /// </summary>
    [Required]
    public decimal TransactionFee { get; private set; }

    /// <summary>
    /// Gets the 8-digit transaction code for completing the transfer
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string TransactionCode { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the current status of the transfer
    /// </summary>
    [Required]
    public TransferStatus Status { get; private set; }

    /// <summary>
    /// Gets the fraud risk level assessed by fraud detection service
    /// </summary>
    public RiskLevel? RiskLevel { get; private set; }

    /// <summary>
    /// Gets the idempotency key for preventing duplicate transfers
    /// </summary>
    [MaxLength(100)]
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTime? ApprovalRequiredUntil { get; private set; }

    public string? SenderNationalId { get; private set; }

    public string? ReceiverNationalId { get; private set; }

    // For EF Core
    private Transfer() { }

    public static Transfer Create(
        Guid senderId,
        Guid receiverId,
        decimal amount,
        Currency currency,
        decimal amountInTRY,
        decimal? exchangeRate,
        decimal transactionFee,
        string transactionCode,
        RiskLevel? riskLevel,
        string? idempotencyKey,
        DateTime? approvalRequiredUntil,
        string senderNationalId,
        string receiverNationalId)
    {
        var transfer = new Transfer
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Amount = amount,
            Currency = currency,
            AmountInTRY = amountInTRY,
            ExchangeRate = exchangeRate,
            TransactionFee = transactionFee,
            TransactionCode = transactionCode,
            Status = TransferStatus.Pending,
            RiskLevel = riskLevel,
            IdempotencyKey = idempotencyKey,
            ApprovalRequiredUntil = approvalRequiredUntil,
            SenderNationalId = senderNationalId,
            ReceiverNationalId = receiverNationalId,
            CreatedAt = DateTime.UtcNow
        };

        return transfer;
    }

    public static Transfer CreateFailed(
        Guid senderId,
        Guid receiverId,
        decimal amount,
        Currency currency,
        decimal amountInTRY,
        decimal? exchangeRate,
        string transactionCode,
        RiskLevel? riskLevel,
        string? idempotencyKey,
        string senderNationalId,
        string receiverNationalId)
    {
        return new Transfer
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Amount = amount,
            Currency = currency,
            AmountInTRY = amountInTRY,
            ExchangeRate = exchangeRate,
            TransactionFee = 0,
            TransactionCode = transactionCode,
            Status = TransferStatus.Failed,
            RiskLevel = riskLevel,
            IdempotencyKey = idempotencyKey,
            SenderNationalId = senderNationalId,
            ReceiverNationalId = receiverNationalId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete()
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException($"Cannot complete transfer with status {Status}");

        if (ApprovalRequiredUntil.HasValue && ApprovalRequiredUntil.Value > DateTime.UtcNow)
        {
            var remainingMinutes = (ApprovalRequiredUntil.Value - DateTime.UtcNow).TotalMinutes;
            throw new InvalidOperationException(
                $"Transfer approval required. Please wait {Math.Ceiling(remainingMinutes)} more minute(s)");
        }

        Status = TransferStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel(string? reason)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel transfer with status {Status}");

        Status = TransferStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
    }

    public bool RequiresApproval()
    {
        return ApprovalRequiredUntil.HasValue && ApprovalRequiredUntil.Value > DateTime.UtcNow;
    }

    public bool IsHighValue()
    {
        return AmountInTRY > 1000m;
    }
}
