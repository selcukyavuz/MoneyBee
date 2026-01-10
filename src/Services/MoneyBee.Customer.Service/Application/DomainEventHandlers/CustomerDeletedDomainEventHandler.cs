using MoneyBee.Common.DDD;
using MoneyBee.Common.Events;
using MoneyBee.Customer.Service.Domain.Events;
using MoneyBee.Customer.Service.Infrastructure.Messaging;

namespace MoneyBee.Customer.Service.Application.DomainEventHandlers;

/// <summary>
/// Handles CustomerDeletedDomainEvent and publishes to message bus
/// </summary>
public class CustomerDeletedDomainEventHandler : IDomainEventHandler<CustomerDeletedDomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CustomerDeletedDomainEventHandler> _logger;

    public CustomerDeletedDomainEventHandler(
        IEventPublisher eventPublisher,
        ILogger<CustomerDeletedDomainEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(CustomerDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling CustomerDeletedDomainEvent for customer {CustomerId}",
            domainEvent.CustomerId);

        // Convert to integration event
        var integrationEvent = new CustomerDeletedEvent
        {
            CustomerId = domainEvent.CustomerId,
            NationalId = domainEvent.NationalId,
            Timestamp = domainEvent.OccurredOn,
            CorrelationId = domainEvent.EventId.ToString()
        };

        await _eventPublisher.PublishAsync(integrationEvent);

        _logger.LogInformation(
            "CustomerDeletedEvent published to message bus for customer {CustomerId}",
            domainEvent.CustomerId);

        // Additional domain logic:
        // - Archive customer data
        // - Cancel pending transfers
        // - Send notification
        // - Update reports
    }
}
