using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MoneyBee.Common.Constants;
using MoneyBee.Common.Events;
using MoneyBee.Common.Options;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace MoneyBee.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests with Testcontainers
/// Provides PostgreSQL, Redis, RabbitMQ containers, and API key authentication
/// For single-service tests, RabbitMQ is replaced with a mock
/// </summary>
public class IntegrationTestFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;
    private readonly bool _useRabbitMq;
    private WebApplicationFactory<MoneyBee.Auth.Service.Program>? _authFactory;
    private HttpClient? _authClient;

    public string PostgresConnectionString { get; private set; } = string.Empty;
    public string RedisConnectionString { get; private set; } = string.Empty;
    public string AuthServiceUrl { get; private set; } = string.Empty;
    public string TestApiKey { get; private set; } = string.Empty;

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

        // Start Auth Service to generate API keys
        await StartAuthServiceAsync();
    }

    private async Task StartAuthServiceAsync()
    {
        // Create a separate PostgreSQL container for Auth Service
        var authPostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("auth_test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        await authPostgresContainer.StartAsync();

        var authPostgresConnectionString = authPostgresContainer.GetConnectionString();

        // Create Auth Service factory
        _authFactory = new WebApplicationFactory<MoneyBee.Auth.Service.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = authPostgresConnectionString,
                        ["Redis:ConnectionString"] = RedisConnectionString
                    });
                });
                builder.UseEnvironment("Testing");
            });

        _authClient = _authFactory.CreateClient();
        // Store the Auth Service base address for configuration
        // Note: WebApplicationFactory clients use in-memory test server, so we use the factory itself
        AuthServiceUrl = _authClient.BaseAddress?.ToString() ?? "http://localhost:5001";

        // Create a test API key
        var createKeyRequest = new
        {
            name = "Integration Test Key",
            description = "API Key for integration tests",
            expiresAt = DateTime.UtcNow.AddDays(1)
        };

        var response = await _authClient.PostAsJsonAsync("/api/auth/keys", createKeyRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponseWrapper>();
        TestApiKey = result?.Data?.ApiKey ?? throw new Exception("Failed to create test API key");
    }

    /// <summary>
    /// Wrapper for ApiResponse returned by Auth Service
    /// </summary>
    private class ApiResponseWrapper
    {
        public bool Success { get; set; }
        public ApiKeyData? Data { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// The actual CreateApiKeyResponse data nested inside ApiResponse
    /// </summary>
    private class ApiKeyData
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration with test values
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = PostgresConnectionString,
                ["ConnectionStrings:Redis"] = RedisConnectionString,
                ["Redis:ConnectionString"] = RedisConnectionString,
                ["Services:AuthService:Url"] = AuthServiceUrl,
                // Dummy RabbitMQ config to prevent connection attempts
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Port"] = "5672",
                ["RabbitMQ:Username"] = "test",
                ["RabbitMQ:Password"] = "test"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace RabbitMQ event publisher with mock for single-service tests
            if (!_useRabbitMq)
            {
                // Remove real EventPublisher (Customer Service)
                var eventPublisherDescriptor = services.FirstOrDefault(d => 
                    d.ServiceType == typeof(MoneyBee.Common.Abstractions.IEventPublisher));
                if (eventPublisherDescriptor != null)
                {
                    services.Remove(eventPublisherDescriptor);
                }

                // Add mock EventPublisher
                services.AddSingleton<MoneyBee.Common.Abstractions.IEventPublisher, MockEventPublisher>();
            }

            // Replace Auth Service HTTP client with the one that uses WebApplicationFactory
            // This allows the test services to call the Auth Service test instance
            services.RemoveAll<MoneyBee.Common.Abstractions.IApiKeyValidator>();
            services.AddSingleton<MoneyBee.Common.Abstractions.IApiKeyValidator>(sp => 
                new MoneyBee.Common.Infrastructure.Caching.CachedApiKeyValidator(
                    sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(),
                    _authClient ?? throw new InvalidOperationException("Auth client not initialized"),
                    sp.GetRequiredService<ILogger<MoneyBee.Common.Infrastructure.Caching.CachedApiKeyValidator>>()
                )
            );
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates an HttpClient with API key authentication header
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        if (!string.IsNullOrEmpty(TestApiKey))
        {
            client.DefaultRequestHeaders.Add(HttpHeaders.ApiKey, TestApiKey);
        }
        return client;
    }

    public new async Task DisposeAsync()
    {
        // Dispose Auth Service
        if (_authClient != null)
        {
            _authClient.Dispose();
        }
        if (_authFactory != null)
        {
            await _authFactory.DisposeAsync();
        }

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
public class MockEventPublisher : MoneyBee.Common.Abstractions.IEventPublisher
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
