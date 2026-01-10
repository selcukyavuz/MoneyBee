using System.ComponentModel.DataAnnotations;
using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Entities;

public class Transfer
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SenderId { get; set; }

    [Required]
    public Guid ReceiverId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Required]
    public decimal AmountInTRY { get; set; }

    public decimal? ExchangeRate { get; set; }

    [Required]
    public decimal TransactionFee { get; set; }

    [Required]
    [MaxLength(8)]
    public string TransactionCode { get; set; } = string.Empty;

    [Required]
    public TransferStatus Status { get; set; }

    public RiskLevel? RiskLevel { get; set; }

    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime? ApprovalRequiredUntil { get; set; }

    public string? SenderNationalId { get; set; }

    public string? ReceiverNationalId { get; set; }
}
