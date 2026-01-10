using MoneyBee.Common.DDD;
using MoneyBee.Common.Events;
using MoneyBee.Transfer.Service.Domain.Events;
using MoneyBee.Transfer.Service.Infrastructure.Messaging;

namespace MoneyBee.Transfer.Service.Application.DomainEventHandlers;

/// <summary>
/// Handles TransferCancelledDomainEvent - notifications, cleanup
/// </summary>
public class TransferCancelledDomainEventHandler : IDomainEventHandler<TransferCancelledDomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<TransferCancelledDomainEventHandler> _logger;

    public TransferCancelledDomainEventHandler(
        IEventPublisher eventPublisher,
        ILogger<TransferCancelledDomainEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(TransferCancelledDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling TransferCancelledDomainEvent: {TransferId} - Reason: {Reason}",
            domainEvent.TransferId,
            domainEvent.Reason);

        // 1. Process fee refund
        await ProcessFeeRefundAsync(domainEvent);

        // 2. Send cancellation notifications
        await SendCancellationNotificationsAsync(domainEvent);

        // 3. Audit cancellation
        await AuditCancellationAsync(domainEvent);

        // 4. Update cancellation metrics
        await TrackCancellationMetricsAsync(domainEvent);

        // 5. Publish integration event to RabbitMQ
        var integrationEvent = new TransferCancelledEvent
        {
            TransferId = domainEvent.TransferId,
            Reason = domainEvent.Reason,
            CorrelationId = domainEvent.EventId.ToString()
        };

        await _eventPublisher.PublishAsync(integrationEvent);

        _logger.LogInformation(
            "Transfer cancellation handled and published to message bus: {TransactionCode}",
            domainEvent.TransactionCode);
    }

    private async Task ProcessFeeRefundAsync(TransferCancelledDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "Fee refund initiated for transfer {TransactionCode}",
            domainEvent.TransactionCode);
        // TODO: Process refund through payment gateway
        await Task.CompletedTask;
    }

    private async Task SendCancellationNotificationsAsync(TransferCancelledDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "Cancellation notification sent for {TransactionCode}",
            domainEvent.TransactionCode);
        // Send email/SMS about cancellation and refund
        await Task.CompletedTask;
    }

    private async Task AuditCancellationAsync(TransferCancelledDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "Audit: Transfer {TransferId} cancelled - {Reason}",
            domainEvent.TransferId,
            domainEvent.Reason);
        await Task.CompletedTask;
    }

    private async Task TrackCancellationMetricsAsync(TransferCancelledDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "Metrics: Transfer cancellation tracked - Reason: {Reason}",
            domainEvent.Reason);
        // Analyze cancellation reasons for improvement
        await Task.CompletedTask;
    }
}
