using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoneyBee.Common.DDD;
using MoneyBee.Common.Persistence;
using System.Text.Json;

namespace MoneyBee.Common.Services;

/// <summary>
/// Background service that processes unpublished outbox messages
/// Polls database, publishes events via domain event dispatcher, marks as published
/// </summary>
public class OutboxProcessor<TContext> : BackgroundService where TContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor<TContext>> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 20;
    private readonly int _maxRetryAttempts = 5;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox Processor started. Processing interval: {Interval}s, Batch size: {BatchSize}",
            _processingInterval.TotalSeconds, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        // Fetch unpublished messages (oldest first)
        var messages = await context.Set<OutboxMessage>()
            .Where(m => !m.Published && m.ProcessAttempts < _maxRetryAttempts)
            .OrderBy(m => m.OccurredOn)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
            return;

        _logger.LogDebug("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Deserialize event
                var eventType = Type.GetType(message.EventType);
                if (eventType == null)
                {
                    _logger.LogWarning(
                        "Could not resolve event type: {EventType}. Skipping message {MessageId}",
                        message.EventType, message.Id);
                    
                    message.ProcessAttempts++;
                    message.LastAttemptAt = DateTime.UtcNow;
                    message.LastError = $"Could not resolve event type: {message.EventType}";
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.EventData, eventType) as DomainEvent;
                if (domainEvent == null)
                {
                    _logger.LogWarning(
                        "Could not deserialize event: {MessageId}",
                        message.Id);
                    
                    message.ProcessAttempts++;
                    message.LastAttemptAt = DateTime.UtcNow;
                    message.LastError = "Deserialization failed";
                    continue;
                }

                // Publish event
                await eventDispatcher.DispatchAsync(new[] { domainEvent }, cancellationToken);

                // Mark as published
                message.Published = true;
                message.PublishedAt = DateTime.UtcNow;
                message.LastAttemptAt = DateTime.UtcNow;
                message.LastError = null;

                _logger.LogDebug(
                    "Published event {EventType} from outbox message {MessageId}",
                    message.EventType, message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error publishing event {EventType} from outbox message {MessageId}. Attempt {Attempt}/{MaxAttempts}",
                    message.EventType, message.Id, message.ProcessAttempts + 1, _maxRetryAttempts);

                message.ProcessAttempts++;
                message.LastAttemptAt = DateTime.UtcNow;
                message.LastError = ex.Message.Length > 2000 ? ex.Message.Substring(0, 2000) : ex.Message;
            }
        }

        // Save all updates
        await context.SaveChangesAsync(cancellationToken);

        var publishedCount = messages.Count(m => m.Published);
        var failedCount = messages.Count(m => !m.Published);

        if (publishedCount > 0)
        {
            _logger.LogInformation(
                "Published {PublishedCount} outbox messages. Failed: {FailedCount}",
                publishedCount, failedCount);
        }
    }
}
