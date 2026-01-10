using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Transfer.Service.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MoneyBee.Transfer.Service.Services;

public class CustomerEventConsumer : BackgroundService
{
    private readonly ILogger<CustomerEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    public CustomerEventConsumer(
        ILogger<CustomerEventConsumer> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Customer Event Consumer starting...");
        
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = _configuration["RabbitMQ:Username"] ?? "moneybee",
                Password = _configuration["RabbitMQ:Password"] ?? "moneybee123",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
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

            // Bind queue to exchange with routing key
            _channel.QueueBind(
                queue: "transfer.customer.events",
                exchange: "moneybee.events",
                routingKey: "customer.status.changed");

            _logger.LogInformation("RabbitMQ connection established, queue bound to exchange");
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
                _logger.LogInformation("Received message: {Message}", message);

                var customerEvent = JsonSerializer.Deserialize<CustomerStatusChangedEvent>(message);

                if (customerEvent != null)
                {
                    await ProcessCustomerStatusChangedAsync(customerEvent);
                    
                    // Acknowledge the message
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    
                    _logger.LogInformation("Message processed and acknowledged");
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize message, rejecting");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", message);
                
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
                    transfer.Status = TransferStatus.Cancelled;

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
