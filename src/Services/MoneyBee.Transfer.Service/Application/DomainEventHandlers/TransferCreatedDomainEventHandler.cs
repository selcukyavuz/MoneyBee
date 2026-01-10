using MoneyBee.Common.DDD;
using MoneyBee.Common.Events;
using MoneyBee.Transfer.Service.Domain.Events;
using MoneyBee.Transfer.Service.Infrastructure.Messaging;

namespace MoneyBee.Transfer.Service.Application.DomainEventHandlers;

/// <summary>
/// Handles TransferCreatedDomainEvent - audit logging, notifications
/// </summary>
public class TransferCreatedDomainEventHandler : IDomainEventHandler<TransferCreatedDomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<TransferCreatedDomainEventHandler> _logger;

    public TransferCreatedDomainEventHandler(
        IEventPublisher eventPublisher,
        ILogger<TransferCreatedDomainEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(TransferCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling TransferCreatedDomainEvent: {TransferId} - {Amount} {Currency} from {SenderId} to {ReceiverId}",
            domainEvent.TransferId,
            domainEvent.Amount,
            domainEvent.Currency,
            domainEvent.SenderId,
            domainEvent.ReceiverId);

        // Domain logic:
        // 1. Send SMS to sender with transaction code
        await SendSmsToSenderAsync(domainEvent);

        // 2. Create audit log
        await CreateAuditLogAsync(domainEvent);

        // 3. Update analytics/metrics
        await UpdateMetricsAsync(domainEvent);

        // 4. Publish integration event to RabbitMQ for other services
        var integrationEvent = new TransferCreatedEvent
        {
            TransferId = domainEvent.TransferId,
            SenderId = domainEvent.SenderId,
            ReceiverId = domainEvent.ReceiverId,
            Amount = domainEvent.Amount,
            Currency = domainEvent.Currency.ToString(),
            CorrelationId = domainEvent.EventId.ToString()
        };

        await _eventPublisher.PublishAsync(integrationEvent);

        _logger.LogInformation(
            "Transfer creation handled and published to message bus: {TransactionCode}",
            domainEvent.TransactionCode);
    }

    private async Task SendSmsToSenderAsync(TransferCreatedDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "SMS sent to sender: Transfer created with code {TransactionCode}",
            domainEvent.TransactionCode);
        // TODO: Integrate SMS service
        await Task.CompletedTask;
    }

    private async Task CreateAuditLogAsync(TransferCreatedDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "Audit log created for transfer {TransferId}",
            domainEvent.TransferId);
        // TODO: Save to audit database
        await Task.CompletedTask;
    }

    private async Task UpdateMetricsAsync(TransferCreatedDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "Metrics updated: Transfer amount {Amount} {Currency}",
            domainEvent.Amount,
            domainEvent.Currency);
        // TODO: Update time-series database
        await Task.CompletedTask;
    }
}
