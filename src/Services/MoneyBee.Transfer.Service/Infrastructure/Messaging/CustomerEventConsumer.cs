using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Transfer.Service.Infrastructure.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MoneyBee.Transfer.Service.Infrastructure.Messaging;

public class CustomerEventConsumer : BackgroundService
{
    private readonly ILogger<CustomerEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection? _connection;
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
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ channel not available, consumer will not process messages");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                _logger.LogInformation("Received message with routing key {RoutingKey}", ea.RoutingKey);

                // Route based on event type
                switch (ea.RoutingKey)
                {
                    case "customer.status.changed":
                        var statusChangedEvent = JsonSerializer.Deserialize<CustomerStatusChangedEvent>(message);
                        if (statusChangedEvent != null)
                        {
                            await ProcessCustomerStatusChangedAsync(statusChangedEvent);
                        }
                        break;

                    case "customer.created":
                        var createdEvent = JsonSerializer.Deserialize<CustomerCreatedEvent>(message);
                        if (createdEvent != null)
                        {
                            await ProcessCustomerCreatedAsync(createdEvent);
                        }
                        break;

                    case "customer.deleted":
                        var deletedEvent = JsonSerializer.Deserialize<CustomerDeletedEvent>(message);
                        if (deletedEvent != null)
                        {
                            await ProcessCustomerDeletedAsync(deletedEvent);
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

        _channel.BasicConsume(
            queue: "transfer.customer.events",
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Customer Event Consumer started listening");

        // Keep the task running
        await Task.Delay(Timeout.Infinite, stoppingToken);
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

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

        // Cancel all pending transfers for deleted customer
        var pendingTransfers = await dbContext.Transfers
            .Where(t => (t.SenderId == customerEvent.CustomerId || t.ReceiverId == customerEvent.CustomerId)
                     && t.Status == TransferStatus.Pending)
            .ToListAsync();

        if (pendingTransfers.Any())
        {
            _logger.LogInformation(
                "Found {Count} pending transfers to cancel for deleted customer {CustomerId}",
                pendingTransfers.Count,
                customerEvent.CustomerId);

            foreach (var transfer in pendingTransfers)
            {
                transfer.Cancel($"Customer {customerEvent.CustomerId} was deleted");

                _logger.LogInformation(
                    "Cancelled transfer {TransactionCode} due to customer {CustomerId} being deleted",
                    transfer.TransactionCode,
                    customerEvent.CustomerId);
            }

            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully cancelled {Count} pending transfers for deleted customer {CustomerId}",
                pendingTransfers.Count,
                customerEvent.CustomerId);
        }
        else
        {
            _logger.LogInformation(
                "No pending transfers found for deleted customer {CustomerId}",
                customerEvent.CustomerId);
        }
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
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

            // Find all pending transfers for this customer (as sender or receiver)
            var pendingTransfers = await dbContext.Transfers
                .Where(t => (t.SenderId == customerEvent.CustomerId || t.ReceiverId == customerEvent.CustomerId)
                         && t.Status == TransferStatus.Pending)
                .ToListAsync();

            if (pendingTransfers.Any())
            {
                _logger.LogInformation(
                    "Found {Count} pending transfers to cancel for customer {CustomerId}",
                    pendingTransfers.Count,
                    customerEvent.CustomerId);

                foreach (var transfer in pendingTransfers)
                {
                    transfer.Cancel($"Customer {customerEvent.CustomerId} was blocked");

                    _logger.LogInformation(
                        "Cancelled transfer {TransactionCode} due to customer {CustomerId} being blocked",
                        transfer.TransactionCode,
                        customerEvent.CustomerId);
                }

                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully cancelled {Count} pending transfers for blocked customer {CustomerId}",
                    pendingTransfers.Count,
                    customerEvent.CustomerId);
            }
            else
            {
                _logger.LogInformation(
                    "No pending transfers found for blocked customer {CustomerId}",
                    customerEvent.CustomerId);
            }
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
