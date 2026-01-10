using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyBee.Common.Events;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace MoneyBee.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests with Testcontainers
/// Provides PostgreSQL, Redis, and RabbitMQ containers
/// For single-service tests, RabbitMQ is replaced with a mock
/// </summary>
public class IntegrationTestFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;
    private readonly bool _useRabbitMq;

    public string PostgresConnectionString { get; private set; } = string.Empty;
    public string RedisConnectionString { get; private set; } = string.Empty;

    public IntegrationTestFactory() : this(false)
    {
    }

    private IntegrationTestFactory(bool useRabbitMq)
    {
        _useRabbitMq = useRabbitMq;

        // PostgreSQL Container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        // Redis Container
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start containers in parallel
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync()
        );

        PostgresConnectionString = _postgresContainer.GetConnectionString();
        RedisConnectionString = _redisContainer.GetConnectionString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration with test values
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = PostgresConnectionString,
                ["Redis:ConnectionString"] = RedisConnectionString,
                // Dummy RabbitMQ config to prevent connection attempts
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Port"] = "5672",
                ["RabbitMQ:Username"] = "test",
                ["RabbitMQ:Password"] = "test"
            });
        });

        // Replace RabbitMQ event publisher with mock for single-service tests
        if (!_useRabbitMq)
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove real EventPublisher (Customer Service)
                var eventPublisherDescriptor = services.FirstOrDefault(d => 
                    d.ServiceType == typeof(MoneyBee.Customer.Service.Infrastructure.Messaging.IEventPublisher));
                if (eventPublisherDescriptor != null)
                {
                    services.Remove(eventPublisherDescriptor);
                }

                // Add mock EventPublisher
                services.AddSingleton<MoneyBee.Customer.Service.Infrastructure.Messaging.IEventPublisher, MockEventPublisher>();
            });
        }

        builder.UseEnvironment("Testing");
    }

    public new async Task DisposeAsync()
    {
        // Stop all containers
        await Task.WhenAll(
            _postgresContainer.DisposeAsync().AsTask(),
            _redisContainer.DisposeAsync().AsTask()
        );

        await base.DisposeAsync();
    }
}

/// <summary>
/// Mock EventPublisher for integration tests
/// Doesn't require RabbitMQ connection
/// </summary>
public class MockEventPublisher : MoneyBee.Customer.Service.Infrastructure.Messaging.IEventPublisher
{
    private readonly ILogger<MockEventPublisher> _logger;
    private readonly List<object> _publishedEvents = new();

    public MockEventPublisher(ILogger<MockEventPublisher>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MockEventPublisher>.Instance;
    }

    public Task PublishAsync<T>(T @event) where T : class
    {
        _publishedEvents.Add(@event);
        _logger.LogInformation("Mock: Event published - {EventType}", typeof(T).Name);
        return Task.CompletedTask;
    }

    public IReadOnlyList<object> PublishedEvents => _publishedEvents.AsReadOnly();
}
