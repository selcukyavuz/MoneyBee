using MoneyBee.Common.Enums;
using MoneyBee.Common.Results;
using MoneyBee.Transfer.Service.Constants;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;

namespace MoneyBee.Transfer.Service.Domain.Validators;

/// <summary>
/// Domain validator for Transfer validation and business rules
/// </summary>
public static class TransferValidator
{
    public static bool RequiresApprovalWait(decimal amountInTRY, decimal highAmountThreshold)
    {
        return amountInTRY > highAmountThreshold;
    }

    public static DateTime? CalculateApprovalWaitTime(decimal amountInTRY, decimal highAmountThreshold, int approvalWaitMinutes)
    {
        if (!RequiresApprovalWait(amountInTRY, highAmountThreshold))
            return null;

        return DateTime.UtcNow.AddMinutes(approvalWaitMinutes);
    }

    public static Result ValidateTransferForCompletion(TransferEntity transfer, string receiverNationalId)
    {
        if (transfer.Status != TransferStatus.Pending)
            return Result.Failure(string.Format(ErrorMessages.Transfer.CannotBeCompleted, transfer.Status));

        if (transfer.ReceiverNationalId != receiverNationalId)
            return Result.Failure(ErrorMessages.Transfer.ReceiverVerificationFailed);

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
            return Result.Failure(string.Format(ErrorMessages.Transfer.DailyLimitExceeded, remainingLimit));

        return Result.Success();
    }

    public static bool ShouldRejectTransfer(RiskLevel? riskLevel)
    {
        return riskLevel == RiskLevel.High;
    }
}
