using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Common.Serialization;
using MoneyBee.Transfer.Service.Domain.Transfers;
using MoneyBee.Transfer.Service.Infrastructure.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MoneyBee.Transfer.Service.Infrastructure.Messaging;

public class CustomerEventConsumer : BackgroundService
{
    private readonly ILogger<CustomerEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection? _connection;
    private readonly ConcurrentDictionary<Guid, bool> _processedEvents = new();
    private IModel? _channel;

    public CustomerEventConsumer(
        ILogger<CustomerEventConsumer> logger,
        IServiceProvider serviceProvider,
        IConnection? connection)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _connection = connection;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Customer Event Consumer starting...");
        
        if (_connection == null)
        {
            _logger.LogWarning("RabbitMQ connection is not available. Customer Event Consumer will not start.");
            return Task.CompletedTask;
        }

        try
        {
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: "moneybee.events",
                type: ExchangeType.Topic,
                durable: true);

            // Declare queue
            _channel.QueueDeclare(
                queue: "transfer.customer.events",
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind queue to exchange with routing keys
            _channel.QueueBind(
                queue: "transfer.customer.events",
                exchange: "moneybee.events",
                routingKey: "customer.status.changed");

            _channel.QueueBind(
                queue: "transfer.customer.events",
                exchange: "moneybee.events",
                routingKey: "customer.created");

            _channel.QueueBind(
                queue: "transfer.customer.events",
                exchange: "moneybee.events",
                routingKey: "customer.deleted");

            // Set prefetch count to process messages one at a time
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ connection established, queue bound to exchange with multiple routing keys");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
        }

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== ExecuteAsync started ===");
        
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ channel not available, consumer will not process messages");
            return;
        }

        _logger.LogInformation("Creating EventingBasicConsumer (synchronous)...");
        var consumer = new EventingBasicConsumer(_channel);

        _logger.LogInformation("Registering Received event handler...");
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("=== CONSUMER RECEIVED EVENT === Routing Key: {RoutingKey}, Message Length: {Length}", 
                ea.RoutingKey, message.Length);

            try
            {
                _logger.LogInformation("Received message with routing key {RoutingKey}", ea.RoutingKey);

                // Route based on event type
                switch (ea.RoutingKey)
                {
                    case "customer.status.changed":
                        var statusChangedEvent = JsonSerializer.Deserialize<CustomerStatusChangedEvent>(message, JsonSerializerOptionsProvider.Default);
                        if (statusChangedEvent != null && _processedEvents.TryAdd(statusChangedEvent.EventId, true))
                        {
                            ProcessCustomerStatusChangedAsync(statusChangedEvent).GetAwaiter().GetResult();
                        }
                        break;

                    case "customer.created":
                        var createdEvent = JsonSerializer.Deserialize<CustomerCreatedEvent>(message, JsonSerializerOptionsProvider.Default);
                        if (createdEvent != null && _processedEvents.TryAdd(createdEvent.EventId, true))
                        {
                            ProcessCustomerCreatedAsync(createdEvent).GetAwaiter().GetResult();
                        }
                        break;

                    case "customer.deleted":
                        var deletedEvent = JsonSerializer.Deserialize<CustomerDeletedEvent>(message, JsonSerializerOptionsProvider.Default);
                        if (deletedEvent != null && _processedEvents.TryAdd(deletedEvent.EventId, true))
                        {
                            ProcessCustomerDeletedAsync(deletedEvent).GetAwaiter().GetResult();
                        }
                        break;

                    default:
                        _logger.LogWarning("Unknown routing key: {RoutingKey}", ea.RoutingKey);
                        break;
                }

                // Acknowledge the message
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Message processed and acknowledged");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message with routing key {RoutingKey}", ea.RoutingKey);
                
                // Reject and don't requeue if processing fails
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _logger.LogInformation("Calling BasicConsume...");
        var consumerTag = _channel.BasicConsume(
            queue: "transfer.customer.events",
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Consumer registered with tag: {ConsumerTag}", consumerTag);
        _logger.LogInformation("Customer Event Consumer started listening on queue 'transfer.customer.events'");
        _logger.LogInformation("Waiting for customer events...");

        // Keep the task running
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Consumer task cancelled");
        }
    }

    private async Task ProcessCustomerCreatedAsync(CustomerCreatedEvent customerEvent)
    {
        _logger.LogInformation(
            "Processing customer created: Customer {CustomerId} - {FirstName} {LastName}",
            customerEvent.CustomerId,
            customerEvent.FirstName,
            customerEvent.LastName);

        // Business logic: Customer created, might need to update local cache or trigger welcome flow
        // For now, just log the event
        await Task.CompletedTask;

        _logger.LogInformation(
            "Customer created event processed for {CustomerId}",
            customerEvent.CustomerId);
    }

    private async Task ProcessCustomerDeletedAsync(CustomerDeletedEvent customerEvent)
    {
        _logger.LogInformation(
            "Processing customer deleted: Customer {CustomerId}",
            customerEvent.CustomerId);

        await CancelCustomerPendingTransfersAsync(
            customerEvent.CustomerId,
            $"Customer {customerEvent.CustomerId} was deleted");

        _logger.LogInformation(
            "Customer deleted event processed for {CustomerId}",
            customerEvent.CustomerId);
    }

    private async Task ProcessCustomerStatusChangedAsync(CustomerStatusChangedEvent customerEvent)
    {
        _logger.LogInformation(
            "Processing customer status change: Customer {CustomerId} changed from {OldStatus} to {NewStatus}",
            customerEvent.CustomerId,
            customerEvent.PreviousStatus,
            customerEvent.NewStatus);
    
            // If customer is blocked, cancel all pending transfers
            if (customerEvent.NewStatus == CustomerStatus.Blocked.ToString())
            {
                _logger.LogWarning("Customer {CustomerId} was blocked, cancelling pending transfers", customerEvent.CustomerId);
                await CancelCustomerPendingTransfersAsync(
                    customerEvent.CustomerId,
                    $"Customer {customerEvent.CustomerId} was blocked");
            }

        _logger.LogInformation(
            "Customer status changed event processed for {CustomerId}",
            customerEvent.CustomerId);
    }

    private async Task CancelCustomerPendingTransfersAsync(Guid customerId, string reason)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransferRepository>();

        var pendingTransfers = await repository.GetPendingByCustomerIdAsync(customerId);
        var transfersList = pendingTransfers.ToList();

        if (transfersList.Any())
        {
            _logger.LogInformation(
                "Found {Count} pending transfers to cancel for customer {CustomerId}",
                transfersList.Count,
                customerId);

            foreach (var transfer in transfersList)
            {
                transfer.Cancel(reason);
                await repository.UpdateAsync(transfer);

                _logger.LogInformation(
                    "Cancelled transfer {TransactionCode}: {Reason}",
                    transfer.TransactionCode,
                    reason);
            }

            _logger.LogInformation(
                "Successfully cancelled {Count} pending transfers for customer {CustomerId}",
                transfersList.Count,
                customerId);
        }
        else
        {
            _logger.LogInformation(
                "No pending transfers found for customer {CustomerId}",
                customerId);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Customer Event Consumer stopping...");
        
        _channel?.Close();
        _connection?.Close();
        
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
