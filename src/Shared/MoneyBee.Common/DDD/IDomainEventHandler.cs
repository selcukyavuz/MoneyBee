namespace MoneyBee.Common.DDD;

/// <summary>
/// Interface for handling domain events
/// </summary>
public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : DomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
