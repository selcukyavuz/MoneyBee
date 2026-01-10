using MoneyBee.Common.Enums;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;

namespace MoneyBee.Transfer.Service.Domain.Services;

/// <summary>
/// Domain Service for Transfer orchestration and business rules
/// </summary>
public class TransferDomainService
{
    private const decimal HIGH_AMOUNT_THRESHOLD_TRY = 1000m;

    public bool RequiresApprovalWait(decimal amountInTRY)
    {
        return amountInTRY > HIGH_AMOUNT_THRESHOLD_TRY;
    }

    public DateTime? CalculateApprovalWaitTime(decimal amountInTRY)
    {
        if (!RequiresApprovalWait(amountInTRY))
            return null;

        return DateTime.UtcNow.AddMinutes(5);
    }

    public void ValidateTransferForCompletion(TransferEntity transfer, string receiverNationalId)
    {
        if (transfer.Status != TransferStatus.Pending)
            throw new InvalidOperationException($"Transfer cannot be completed. Status: {transfer.Status}");

        if (transfer.ReceiverNationalId != receiverNationalId)
            throw new InvalidOperationException("Receiver identity verification failed");

        if (transfer.RequiresApproval())
        {
            var remainingMinutes = (transfer.ApprovalRequiredUntil!.Value - DateTime.UtcNow).TotalMinutes;
            throw new InvalidOperationException(
                $"Transfer approval required. Please wait {Math.Ceiling(remainingMinutes)} more minute(s)");
        }
    }

    public void ValidateDailyLimit(decimal currentDailyTotal, decimal newAmount, decimal dailyLimit)
    {
        var remainingLimit = dailyLimit - currentDailyTotal;
        
        if (remainingLimit < newAmount)
            throw new InvalidOperationException($"Daily transfer limit exceeded. Remaining: {remainingLimit:F2} TRY");
    }

    public bool ShouldRejectTransfer(RiskLevel? riskLevel)
    {
        return riskLevel == RiskLevel.High;
    }
}
