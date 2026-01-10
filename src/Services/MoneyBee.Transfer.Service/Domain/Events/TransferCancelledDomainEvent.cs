using MoneyBee.Common.DDD;

namespace MoneyBee.Transfer.Service.Domain.Events;

public class TransferCancelledDomainEvent : DomainEvent
{
    public Guid TransferId { get; }
    public string TransactionCode { get; }
    public string? Reason { get; }

    public TransferCancelledDomainEvent(Guid transferId, string transactionCode, string? reason)
    {
        TransferId = transferId;
        TransactionCode = transactionCode;
        Reason = reason;
    }
}
