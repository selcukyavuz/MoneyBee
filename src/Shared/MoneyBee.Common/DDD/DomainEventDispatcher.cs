using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MoneyBee.Common.DDD;

/// <summary>
/// Dispatches domain events to registered handlers
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler == null)
                continue;

            try
            {
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    var task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                    await task;
                    
                    _logger.LogInformation(
                        "Domain event {EventType} handled by {HandlerType}",
                        eventType.Name,
                        handler.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error handling domain event {EventType} with handler {HandlerType}",
                    eventType.Name,
                    handler.GetType().Name);
                throw;
            }
        }
    }

    public async Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}
