using MoneyBee.Transfer.Service.Application.Transfers.Commands.CreateTransfer;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Transfers.Transfer;

namespace MoneyBee.Transfer.Service.Application.Transfers.Shared;

public static class TransferMapper
{
    public static CreateTransferResponse ToCreateResponse(TransferEntity transfer)
    {
        return new CreateTransferResponse
        {
            TransferId = transfer.Id,
            TransactionCode = transfer.TransactionCode,
            Status = transfer.Status,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            AmountInTRY = transfer.AmountInTRY,
            TransactionFee = transfer.TransactionFee,
            RiskLevel = transfer.RiskLevel,
            ApprovalRequiredUntil = transfer.ApprovalRequiredUntil,
            Message = transfer.ApprovalRequiredUntil.HasValue
                ? "Transfer created. 5-minute approval wait required for high-value transfers."
                : "Transfer created successfully"
        };
    }

    public static TransferDto ToDto(TransferEntity transfer)
    {
        return new TransferDto
        {
            Id = transfer.Id,
            SenderId = transfer.SenderId,
            ReceiverId = transfer.ReceiverId,
            SenderNationalId = transfer.SenderNationalId,
            ReceiverNationalId = transfer.ReceiverNationalId,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            AmountInTRY = transfer.AmountInTRY,
            ExchangeRate = transfer.ExchangeRate,
            TransactionFee = transfer.TransactionFee,
            TransactionCode = transfer.TransactionCode,
            Status = transfer.Status,
            RiskLevel = transfer.RiskLevel,
            CreatedAt = transfer.CreatedAt,
            CompletedAt = transfer.CompletedAt,
            CancelledAt = transfer.CancelledAt,
            CancellationReason = transfer.CancellationReason,
            ApprovalRequiredUntil = transfer.ApprovalRequiredUntil
        };
    }
}
