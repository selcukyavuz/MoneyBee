using RabbitMQ.Client;
using MoneyBee.Common.Events;
using System.Text;
using System.Text.Json;

namespace MoneyBee.Transfer.Service.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T eventData) where T : class;
}

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private const string ExchangeName = "moneybee.events";

    public RabbitMqEventPublisher(
        IConnection? connection,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;
        _connection = connection;

        if (_connection == null)
        {
            _logger.LogWarning("RabbitMQ connection is not available. Events will not be published.");
            return;
        }

        try
        {
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ channel created for Transfer Service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ channel");
        }
    }

    public async Task PublishAsync<T>(T eventData) where T : class
    {
        if (_channel == null || _connection == null || !_connection.IsOpen)
        {
            _logger.LogWarning("Cannot publish event {EventType}: RabbitMQ is not available", typeof(T).Name);
            return;
        }

        try
        {
            var eventTypeName = typeof(T).Name;
            var routingKey = eventTypeName switch
            {
                "TransferCreatedEvent" => "transfer.created",
                "TransferCompletedEvent" => "transfer.completed",
                "TransferCancelledEvent" => "transfer.cancelled",
                _ => $"transfer.{eventTypeName.ToLower()}"
            };

            var message = JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published {EventType} to {RoutingKey}", eventTypeName, routingKey);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event: {EventType}", typeof(T).Name);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
