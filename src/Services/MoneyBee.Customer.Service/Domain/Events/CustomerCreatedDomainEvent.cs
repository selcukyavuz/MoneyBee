using MoneyBee.Common.DDD;

namespace MoneyBee.Customer.Service.Domain.Events;

public class CustomerCreatedDomainEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public string NationalId { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }

    public CustomerCreatedDomainEvent(Guid customerId, string nationalId, string firstName, string lastName, string email)
    {
        CustomerId = customerId;
        NationalId = nationalId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }
}
