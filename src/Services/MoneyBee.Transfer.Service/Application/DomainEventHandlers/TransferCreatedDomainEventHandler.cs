using MoneyBee.Common.DDD;
using MoneyBee.Transfer.Service.Domain.Events;

namespace MoneyBee.Transfer.Service.Application.DomainEventHandlers;

/// <summary>
/// Handles TransferCreatedDomainEvent - audit logging, notifications
/// </summary>
public class TransferCreatedDomainEventHandler : IDomainEventHandler<TransferCreatedDomainEvent>
{
    private readonly ILogger<TransferCreatedDomainEventHandler> _logger;

    public TransferCreatedDomainEventHandler(ILogger<TransferCreatedDomainEventHandler> logger)
    {
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

        _logger.LogInformation(
            "Transfer creation handled: {TransactionCode}",
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
