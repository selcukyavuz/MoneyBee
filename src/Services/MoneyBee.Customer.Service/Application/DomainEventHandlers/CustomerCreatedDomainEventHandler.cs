using MoneyBee.Common.DDD;
using MoneyBee.Common.Events;
using MoneyBee.Customer.Service.Domain.Events;
using MoneyBee.Customer.Service.Infrastructure.Messaging;

namespace MoneyBee.Customer.Service.Application.DomainEventHandlers;

/// <summary>
/// Handles CustomerCreatedDomainEvent and publishes to message bus
/// </summary>
public class CustomerCreatedDomainEventHandler : IDomainEventHandler<CustomerCreatedDomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CustomerCreatedDomainEventHandler> _logger;

    public CustomerCreatedDomainEventHandler(
        IEventPublisher eventPublisher,
        ILogger<CustomerCreatedDomainEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(CustomerCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling CustomerCreatedDomainEvent for customer {CustomerId}",
            domainEvent.CustomerId);

        // Convert domain event to integration event for RabbitMQ
        var integrationEvent = new CustomerCreatedEvent
        {
            CustomerId = domainEvent.CustomerId,
            NationalId = domainEvent.NationalId,
            FirstName = domainEvent.FirstName,
            LastName = domainEvent.LastName,
            Email = domainEvent.Email,
            Timestamp = domainEvent.OccurredOn,
            CorrelationId = domainEvent.EventId.ToString()
        };

        // Publish to RabbitMQ for other microservices
        await _eventPublisher.PublishAsync(integrationEvent);

        _logger.LogInformation(
            "CustomerCreatedEvent published to message bus for customer {CustomerId}",
            domainEvent.CustomerId);

        // Additional domain logic here:
        // - Send welcome email
        // - Create audit log
        // - Update analytics
        // - Trigger onboarding workflow
    }
}
