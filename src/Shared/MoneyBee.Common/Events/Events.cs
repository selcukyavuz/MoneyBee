namespace MoneyBee.Common.Events;

public abstract class BaseEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
}

public class CustomerStatusChangedEvent : BaseEvent
{
    public Guid CustomerId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class TransferCreatedEvent : BaseEvent
{
    public Guid TransferId { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class TransferCompletedEvent : BaseEvent
{
    public Guid TransferId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
}

public class TransferCancelledEvent : BaseEvent
{
    public Guid TransferId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
