using MoneyBee.Common.DDD;
using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Domain.Events;

public class CustomerStatusChangedDomainEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public CustomerStatus OldStatus { get; }
    public CustomerStatus NewStatus { get; }

    public CustomerStatusChangedDomainEvent(Guid customerId, CustomerStatus oldStatus, CustomerStatus newStatus)
    {
        CustomerId = customerId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
