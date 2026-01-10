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
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private const string ExchangeName = "moneybee.events";

    public RabbitMqEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;

        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = configuration["RabbitMQ:Username"] ?? "moneybee",
                Password = configuration["RabbitMQ:Password"] ?? "moneybee123",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ connection established for Transfer Service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            throw;
        }
    }

    public async Task PublishAsync<T>(T eventData) where T : class
    {
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
