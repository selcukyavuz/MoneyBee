using MoneyBee.Common.Enums;
using MoneyBee.Common.Results;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;

namespace MoneyBee.Transfer.Service.Domain.Validators;

/// <summary>
/// Domain validator for Transfer validation and business rules
/// </summary>
public static class TransferValidator
{
    private const decimal HIGH_AMOUNT_THRESHOLD_TRY = 1000m;

    public static bool RequiresApprovalWait(decimal amountInTRY)
    {
        return amountInTRY > HIGH_AMOUNT_THRESHOLD_TRY;
    }

    public static DateTime? CalculateApprovalWaitTime(decimal amountInTRY)
    {
        if (!RequiresApprovalWait(amountInTRY))
            return null;

        return DateTime.UtcNow.AddMinutes(5);
    }

    public static Result ValidateTransferForCompletion(TransferEntity transfer, string receiverNationalId)
    {
        if (transfer.Status != TransferStatus.Pending)
            return Result.Failure($"Transfer cannot be completed. Status: {transfer.Status}");

        if (transfer.ReceiverNationalId != receiverNationalId)
            return Result.Failure("Receiver identity verification failed");

        if (transfer.RequiresApproval())
        {
            var remainingMinutes = (transfer.ApprovalRequiredUntil!.Value - DateTime.UtcNow).TotalMinutes;
            return Result.Failure(
                $"Transfer approval required. Please wait {Math.Ceiling(remainingMinutes)} more minute(s)");
        }

        return Result.Success();
    }

    public static Result ValidateDailyLimit(decimal currentDailyTotal, decimal newAmount, decimal dailyLimit)
    {
        var remainingLimit = dailyLimit - currentDailyTotal;
        
        if (remainingLimit < newAmount)
            return Result.Failure($"Daily transfer limit exceeded. Remaining: {remainingLimit:F2} TRY");

        return Result.Success();
    }

    public static bool ShouldRejectTransfer(RiskLevel? riskLevel)
    {
        return riskLevel == RiskLevel.High;
    }
}
