using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Common.Serialization;
using MoneyBee.Transfer.Service.Domain.Transfers;
using MoneyBee.Transfer.Service.Infrastructure.Messaging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Transfers.Transfer;

namespace MoneyBee.Transfer.Service.UnitTests.Infrastructure.Messaging;

public class CustomerEventConsumerTests
{
    private readonly Mock<ILogger<CustomerEventConsumer>> _mockLogger;
    private readonly Mock<IConnection> _mockConnection;
    private readonly Mock<IModel> _mockChannel;
    private readonly Mock<ITransferRepository> _mockRepository;
    private readonly IServiceProvider _serviceProvider;

    public CustomerEventConsumerTests()
    {
        _mockLogger = new Mock<ILogger<CustomerEventConsumer>>();
        _mockConnection = new Mock<IConnection>();
        _mockChannel = new Mock<IModel>();
        _mockRepository = new Mock<ITransferRepository>();

        var services = new ServiceCollection();
        services.AddScoped(_ => _mockRepository.Object);
        _serviceProvider = services.BuildServiceProvider();

        _mockConnection.Setup(x => x.CreateModel()).Returns(_mockChannel.Object);
    }

    [Fact]
    public async Task StartAsync_WithNullConnection_ShouldLogWarningAndNotThrow()
    {
        // Arrange
        var consumer = new CustomerEventConsumer(_mockLogger.Object, _serviceProvider, null);

        // Act
        await consumer.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("RabbitMQ connection is not available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithValidConnection_ShouldDeclareQueueAndBindings()
    {
        // Arrange
        var consumer = new CustomerEventConsumer(_mockLogger.Object, _serviceProvider, _mockConnection.Object);

        // Act
        await consumer.StartAsync(CancellationToken.None);

        // Assert
        _mockChannel.Verify(x => x.ExchangeDeclare(
            "moneybee.events",
            ExchangeType.Topic,
            true,
            false,
            null), Times.Once);

        _mockChannel.Verify(x => x.QueueDeclare(
            "transfer.customer.events",
            true,
            false,
            false,
            null), Times.Once);

        _mockChannel.Verify(x => x.QueueBind(
            "transfer.customer.events",
            "moneybee.events",
            "customer.status.changed",
            null), Times.Once);

        _mockChannel.Verify(x => x.QueueBind(
            "transfer.customer.events",
            "moneybee.events",
            "customer.created",
            null), Times.Once);

        _mockChannel.Verify(x => x.QueueBind(
            "transfer.customer.events",
            "moneybee.events",
            "customer.deleted",
            null), Times.Once);

        _mockChannel.Verify(x => x.BasicQos(0, 1, false), Times.Once);
    }

    [Fact]
    public async Task ProcessCustomerCreatedEvent_ShouldLogInformation()
    {
        // Arrange
        var customerEvent = new CustomerCreatedEvent
        {
            EventId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            NationalId = "12345678901",
            Email = "john.doe@example.com"
        };

        var consumer = new CustomerEventConsumer(_mockLogger.Object, _serviceProvider, _mockConnection.Object);
        await consumer.StartAsync(CancellationToken.None);

        // Act - Simulate message received via reflection
        var method = typeof(CustomerEventConsumer)
            .GetMethod("ProcessCustomerCreatedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            await (Task)method.Invoke(consumer, new object[] { customerEvent })!;
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing customer created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessCustomerStatusChanged_WithBlockedStatus_ShouldCancelPendingTransfers()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var statusEvent = new CustomerStatusChangedEvent
        {
            EventId = Guid.NewGuid(),
            CustomerId = customerId,
            PreviousStatus = "Active",
            NewStatus = ((int)CustomerStatus.Blocked).ToString() // Send as int string
        };

        var pendingTransfer = TransferEntity.Create(
            customerId,
            Guid.NewGuid(),
            100m,
            Currency.TRY,
            100m,
            null,
            5m,
            "TXN123",
            RiskLevel.Low,
            "idempotency-key",
            null,
            "12345678901",
            "98765432109");

        _mockRepository.Setup(x => x.GetPendingByCustomerIdAsync(customerId))
            .ReturnsAsync(new[] { pendingTransfer });

        var consumer = new CustomerEventConsumer(_mockLogger.Object, _serviceProvider, _mockConnection.Object);
        await consumer.StartAsync(CancellationToken.None);

        // Act
        var method = typeof(CustomerEventConsumer)
            .GetMethod("ProcessCustomerStatusChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            await (Task)method.Invoke(consumer, new object[] { statusEvent })!;
        }

        // Assert
        _mockRepository.Verify(x => x.GetPendingByCustomerIdAsync(customerId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<TransferEntity>()), Times.Once);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("was blocked")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessCustomerStatusChanged_WithActiveStatus_ShouldNotCancelTransfers()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var statusEvent = new CustomerStatusChangedEvent
        {
            EventId = Guid.NewGuid(),
            CustomerId = customerId,
            PreviousStatus = "Inactive",
            NewStatus = ((int)CustomerStatus.Active).ToString()
        };

        var consumer = new CustomerEventConsumer(_mockLogger.Object, _serviceProvider, _mockConnection.Object);
        await consumer.StartAsync(CancellationToken.None);

        // Act
        var method = typeof(CustomerEventConsumer)
            .GetMethod("ProcessCustomerStatusChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            await (Task)method.Invoke(consumer, new object[] { statusEvent })!;
        }

        // Assert
        _mockRepository.Verify(x => x.GetPendingByCustomerIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<TransferEntity>()), Times.Never);
    }

    [Fact]
    public async Task ProcessCustomerDeleted_ShouldCancelAllPendingTransfers()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var deleteEvent = new CustomerDeletedEvent
        {
            EventId = Guid.NewGuid(),
            CustomerId = customerId
        };

        var pendingTransfers = new[]
        {
            TransferEntity.Create(customerId, Guid.NewGuid(), 100m, Currency.TRY, 100m, null, 5m, "TXN1", RiskLevel.Low, "key1", null, "12345678901", "98765432109"),
            TransferEntity.Create(customerId, Guid.NewGuid(), 200m, Currency.TRY, 200m, null, 5m, "TXN2", RiskLevel.Low, "key2", null, "12345678901", "98765432109")
        };

        _mockRepository.Setup(x => x.GetPendingByCustomerIdAsync(customerId))
            .ReturnsAsync(pendingTransfers);

        var consumer = new CustomerEventConsumer(_mockLogger.Object, _serviceProvider, _mockConnection.Object);
        await consumer.StartAsync(CancellationToken.None);

        // Act
        var method = typeof(CustomerEventConsumer)
            .GetMethod("ProcessCustomerDeletedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            await (Task)method.Invoke(consumer, new object[] { deleteEvent })!;
        }

        // Assert
        _mockRepository.Verify(x => x.GetPendingByCustomerIdAsync(customerId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<TransferEntity>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessCustomerStatusChanged_WithInvalidStatus_ShouldLogWarning()
    {
        // Arrange
        var statusEvent = new CustomerStatusChangedEvent
        {
            EventId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            PreviousStatus = "Active",
            NewStatus = "InvalidStatus"
        };

        var consumer = new CustomerEventConsumer(_mockLogger.Object, _serviceProvider, _mockConnection.Object);
        await consumer.StartAsync(CancellationToken.None);

        // Act
        var method = typeof(CustomerEventConsumer)
            .GetMethod("ProcessCustomerStatusChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            await (Task)method.Invoke(consumer, new object[] { statusEvent })!;
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid NewStatus value")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
