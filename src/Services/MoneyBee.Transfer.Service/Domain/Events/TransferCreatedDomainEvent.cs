using MoneyBee.Common.DDD;
using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Domain.Events;

public class TransferCreatedDomainEvent : DomainEvent
{
    public Guid TransferId { get; }
    public Guid SenderId { get; }
    public Guid ReceiverId { get; }
    public decimal Amount { get; }
    public Currency Currency { get; }
    public string TransactionCode { get; }

    public TransferCreatedDomainEvent(
        Guid transferId, 
        Guid senderId, 
        Guid receiverId, 
        decimal amount, 
        Currency currency,
        string transactionCode)
    {
        TransferId = transferId;
        SenderId = senderId;
        ReceiverId = receiverId;
        Amount = amount;
        Currency = currency;
        TransactionCode = transactionCode;
    }
}
