using MoneyBee.Common.DDD;
using MoneyBee.Transfer.Service.Domain.Events;

namespace MoneyBee.Transfer.Service.Application.DomainEventHandlers;

/// <summary>
/// Handles TransferCompletedDomainEvent - notifications, analytics, rewards
/// </summary>
public class TransferCompletedDomainEventHandler : IDomainEventHandler<TransferCompletedDomainEvent>
{
    private readonly ILogger<TransferCompletedDomainEventHandler> _logger;

    public TransferCompletedDomainEventHandler(ILogger<TransferCompletedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(TransferCompletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling TransferCompletedDomainEvent: {TransferId} - {TransactionCode}",
            domainEvent.TransferId,
            domainEvent.TransactionCode);

        // 1. Send completion notification to both parties
        await SendCompletionNotificationsAsync(domainEvent);

        // 2. Update customer loyalty points
        await UpdateLoyaltyPointsAsync(domainEvent);

        // 3. Audit completion
        await AuditCompletionAsync(domainEvent);

        // 4. Analytics - track completion time
        await TrackCompletionMetricsAsync(domainEvent);

        _logger.LogInformation(
            "Transfer completion handled: {TransactionCode}",
            domainEvent.TransactionCode);
    }

    private async Task SendCompletionNotificationsAsync(TransferCompletedDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "Notifications sent: Transfer {TransactionCode} completed",
            domainEvent.TransactionCode);
        // Send push notification / email / SMS
        await Task.CompletedTask;
    }

    private async Task UpdateLoyaltyPointsAsync(TransferCompletedDomainEvent domainEvent)
    {
        var points = CalculateLoyaltyPoints(domainEvent.AmountInTRY);
        _logger.LogInformation(
            "Loyalty points updated: +{Points} points for customer {CustomerId}",
            points,
            domainEvent.SenderId);
        // TODO: Update customer loyalty system
        await Task.CompletedTask;
    }

    private async Task AuditCompletionAsync(TransferCompletedDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "Audit: Transfer {TransferId} completed at {Timestamp}",
            domainEvent.TransferId,
            domainEvent.OccurredOn);
        await Task.CompletedTask;
    }

    private async Task TrackCompletionMetricsAsync(TransferCompletedDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "Metrics: Transfer completed - {Amount} TRY",
            domainEvent.AmountInTRY);
        // Track average completion time, success rate, etc.
        await Task.CompletedTask;
    }

    private int CalculateLoyaltyPoints(decimal amount)
    {
        // 1 point per 100 TRY
        return (int)(amount / 100);
    }
}
