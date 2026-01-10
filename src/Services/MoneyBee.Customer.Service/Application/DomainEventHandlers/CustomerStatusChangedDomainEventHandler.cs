using MoneyBee.Common.DDD;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Customer.Service.Domain.Events;
using MoneyBee.Customer.Service.Infrastructure.Messaging;

namespace MoneyBee.Customer.Service.Application.DomainEventHandlers;

/// <summary>
/// Handles CustomerStatusChangedDomainEvent - publishes to message bus and performs domain logic
/// </summary>
public class CustomerStatusChangedDomainEventHandler : IDomainEventHandler<CustomerStatusChangedDomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CustomerStatusChangedDomainEventHandler> _logger;

    public CustomerStatusChangedDomainEventHandler(
        IEventPublisher eventPublisher,
        ILogger<CustomerStatusChangedDomainEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(CustomerStatusChangedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling CustomerStatusChangedDomainEvent for customer {CustomerId}: {OldStatus} -> {NewStatus}",
            domainEvent.CustomerId,
            domainEvent.OldStatus,
            domainEvent.NewStatus);

        // Convert to integration event
        var integrationEvent = new CustomerStatusChangedEvent
        {
            CustomerId = domainEvent.CustomerId,
            PreviousStatus = domainEvent.OldStatus.ToString(),
            NewStatus = domainEvent.NewStatus.ToString(),
            Reason = $"Status changed from {domainEvent.OldStatus} to {domainEvent.NewStatus}",
            CorrelationId = domainEvent.EventId.ToString()
        };

        await _eventPublisher.PublishAsync(integrationEvent);

        // Domain-specific logic based on status change
        await HandleStatusSpecificLogicAsync(domainEvent);

        _logger.LogInformation(
            "CustomerStatusChangedEvent published for customer {CustomerId}",
            domainEvent.CustomerId);
    }

    private async Task HandleStatusSpecificLogicAsync(CustomerStatusChangedDomainEvent domainEvent)
    {
        switch (domainEvent.NewStatus)
        {
            case CustomerStatus.Blocked:
                _logger.LogWarning(
                    "Customer {CustomerId} blocked - Transfer service will cancel pending transfers",
                    domainEvent.CustomerId);
                // Send notification to customer
                // Alert compliance team
                break;

            case CustomerStatus.Active when domainEvent.OldStatus == CustomerStatus.Blocked:
                _logger.LogInformation(
                    "Customer {CustomerId} reactivated from blocked status",
                    domainEvent.CustomerId);
                // Send welcome back notification
                // Resume services
                break;

            case CustomerStatus.Passive:
                _logger.LogInformation(
                    "Customer {CustomerId} moved to passive status",
                    domainEvent.CustomerId);
                // Send notification
                break;
        }

        await Task.CompletedTask;
    }
}
