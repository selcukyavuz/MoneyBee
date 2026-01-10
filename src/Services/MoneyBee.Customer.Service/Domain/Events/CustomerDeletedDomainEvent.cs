using MoneyBee.Common.DDD;

namespace MoneyBee.Customer.Service.Domain.Events;

public class CustomerDeletedDomainEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public string NationalId { get; }

    public CustomerDeletedDomainEvent(Guid customerId, string nationalId)
    {
        CustomerId = customerId;
        NationalId = nationalId;
    }
}
