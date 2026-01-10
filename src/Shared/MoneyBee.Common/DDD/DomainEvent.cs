namespace MoneyBee.Common.DDD;

/// <summary>
/// Base class for Domain Events following DDD principles
/// </summary>
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
