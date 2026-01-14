using MoneyBee.Common.Enums;
using MoneyBee.Common.Results;

namespace MoneyBee.Transfer.Service.Domain.Transfers;

/// <summary>
/// Represents a money transfer transaction with fraud detection and business rules
/// </summary>
public class Transfer
{
    /// <summary>
    /// Gets the unique identifier of the transfer
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the sender customer's unique identifier
    /// </summary>
    public Guid SenderId { get; private set; }

    /// <summary>
    /// Gets the receiver customer's unique identifier
    /// </summary>
    public Guid ReceiverId { get; private set; }

    /// <summary>
    /// Gets the transfer amount in original currency
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Gets the currency of the transfer
    /// </summary>
    public Currency Currency { get; private set; }

    /// <summary>
    /// Gets the transfer amount converted to TRY for limit checks
    /// </summary>
    public decimal AmountInTRY { get; private set; }

    /// <summary>
    /// Gets the exchange rate used for currency conversion
    /// </summary>
    public decimal? ExchangeRate { get; private set; }

    /// <summary>
    /// Gets the transaction fee charged
    /// </summary>
    public decimal TransactionFee { get; private set; }

    /// <summary>
    /// Gets the 8-digit transaction code for completing the transfer
    /// </summary>
    public string TransactionCode { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the current status of the transfer
    /// </summary>
    public TransferStatus Status { get; private set; }

    /// <summary>
    /// Gets the fraud risk level assessed by fraud detection service
    /// </summary>
    public RiskLevel? RiskLevel { get; private set; }

    /// <summary>
    /// Gets the idempotency key for preventing duplicate transfers
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
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

    /// <summary>
    /// Validates if this transfer can be completed
    /// </summary>
    public Result ValidateForCompletion()
    {
        if (Status != TransferStatus.Pending)
            return Result.Failure($"Transfer cannot be completed. Status: {Status}");

        if (RequiresApproval())
        {
            var remainingMinutes = (ApprovalRequiredUntil!.Value - DateTime.UtcNow).TotalMinutes;
            return Result.Failure(
                $"Transfer approval required. Please wait {Math.Ceiling(remainingMinutes)} more minute(s)");
        }

        return Result.Success();
    }

    /// <summary>
    /// Checks if the transfer amount would exceed the daily limit
    /// </summary>
    public static Result ValidateDailyLimit(decimal currentDailyTotal, decimal newAmount, decimal dailyLimit)
    {
        var remainingLimit = dailyLimit - currentDailyTotal;
        
        if (remainingLimit < newAmount)
            return Result.Failure($"Daily transfer limit exceeded. Remaining: {remainingLimit:F2} TRY");

        return Result.Success();
    }

    /// <summary>
    /// Determines if transfer should be rejected based on fraud risk level
    /// </summary>
    public bool ShouldBeRejectedDueToFraud()
    {
        return RiskLevel == Common.Enums.RiskLevel.High;
    }

    /// <summary>
    /// Checks if approval wait is required for this transfer amount
    /// </summary>
    public static bool RequiresApprovalWait(decimal amountInTRY, decimal highAmountThreshold)
    {
        return amountInTRY > highAmountThreshold;
    }

    /// <summary>
    /// Calculates when approval wait will expire
    /// </summary>
    public static DateTime? CalculateApprovalWaitTime(decimal amountInTRY, decimal highAmountThreshold, int approvalWaitMinutes)
    {
        if (!RequiresApprovalWait(amountInTRY, highAmountThreshold))
            return null;

        return DateTime.UtcNow.AddMinutes(approvalWaitMinutes);
    }

    /// <summary>
    /// Calculates the transaction fee based on transfer amount
    /// Fee structure: Base fee + percentage of transfer amount
    /// </summary>
    /// <param name="amountInTRY">The transfer amount in TRY</param>
    /// <param name="baseFee">Base fee to apply</param>
    /// <param name="feePercentage">Percentage fee (e.g., 0.01 for 1%)</param>
    /// <returns>Total fee rounded to 2 decimal places</returns>
    public static decimal CalculateFee(decimal amountInTRY, decimal baseFee, decimal feePercentage)
    {
        var percentageFee = amountInTRY * feePercentage;
        var totalFee = baseFee + percentageFee;
        return Math.Round(totalFee, 2);
    }
}
