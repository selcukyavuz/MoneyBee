using MoneyBee.Common.DDD;

namespace MoneyBee.Transfer.Service.Domain.Events;

public class TransferCompletedDomainEvent : DomainEvent
{
    public Guid TransferId { get; }
    public string TransactionCode { get; }
    public Guid SenderId { get; }
    public Guid ReceiverId { get; }
    public decimal AmountInTRY { get; }

    public TransferCompletedDomainEvent(
        Guid transferId, 
        string transactionCode, 
        Guid senderId, 
        Guid receiverId,
        decimal amountInTRY)
    {
        TransferId = transferId;
        TransactionCode = transactionCode;
        SenderId = senderId;
        ReceiverId = receiverId;
        AmountInTRY = amountInTRY;
    }
}
