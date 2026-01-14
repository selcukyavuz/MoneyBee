using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyBee.Common.Options;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using Serilog;
using StackExchange.Redis;

namespace MoneyBee.Web.Common.Extensions;

/// <summary>
/// Extension methods for configuring Program.cs in microservices
/// </summary>
public static class ProgramExtensions
{
    /// <summary>
    /// Configure Swagger with API Key authentication
    /// </summary>
    public static IServiceCollection AddSwaggerWithApiKey(
        this IServiceCollection services, 
        string title, 
        string description)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { 
                Title = title, 
                Version = "v1",
                Description = description
            });
            c.AddSecurityDefinition("ApiKey", new()
            {
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Name = MoneyBee.Common.Constants.HttpHeaders.ApiKey,
                Description = "API Key for authentication"
            });
            c.AddSecurityRequirement(new()
            {
                {
                    new()
                    {
                        Reference = new()
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        return services;
    }

    /// <summary>
    /// Configure RabbitMQ connection with automatic recovery
    /// </summary>
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection("RabbitMQ"));

        services.AddSingleton<IConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<IConnection>>();
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = options.Host,
                    UserName = options.Username,
                    Password = options.Password,
                    AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(options.NetworkRecoveryIntervalSeconds)
                };
                
                var connection = factory.CreateConnection();
                logger.LogInformation("RabbitMQ connection established to {Host}", options.Host);
                return connection;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to connect to RabbitMQ. Service will start without message queue support.");
                return null!;
            }
        });

        return services;
    }

    /// <summary>
    /// Configure Redis cache with instance name
    /// </summary>
    public static IServiceCollection AddRedisCacheWithInstance(
        this IServiceCollection services,
        IConfiguration configuration,
        string instanceName)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            options.InstanceName = $"{instanceName}:";
        });

        return services;
    }

    /// <summary>
    /// Configure Redis ConnectionMultiplexer for distributed locking
    /// </summary>
    public static IServiceCollection AddRedisConnectionMultiplexer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConfig = ConfigurationOptions.Parse(
            configuration["Redis:ConnectionString"] ?? "localhost:6379");
        redisConfig.AbortOnConnectFail = false;
        
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConfig));

        return services;
    }

    /// <summary>
    /// Add HTTP client with retry policy
    /// </summary>
    public static IHttpClientBuilder AddHttpClientWithRetry<TClient, TImplementation>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient> configureClient,
        int retryCount = 3)
        where TClient : class
        where TImplementation : class, TClient
    {
        var builder = services.AddHttpClient<TClient, TImplementation>();
        
        // Configure the HttpClient  
        builder.ConfigureHttpClient(configureClient);
        
        return builder
            // Removed ConfigurePrimaryHttpMessageHandler to test if it's interfering
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(retryCount, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
    }

    /// <summary>
    /// Add HTTP client with circuit breaker policy
    /// </summary>
    public static IHttpClientBuilder AddHttpClientWithCircuitBreaker<TClient, TImplementation>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient> configureClient,
        int handledEventsAllowedBeforeBreaking = 5,
        int durationOfBreakSeconds = 30)
        where TClient : class
        where TImplementation : class, TClient
    {
        var builder = services.AddHttpClient<TClient, TImplementation>();
        
        // CRITICAL: Use ConfigureHttpClient instead of passing to AddHttpClient
        // This ensures the configuration is applied every time an HttpClient is created
        builder.ConfigureHttpClient(configureClient);
        
        return builder
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            })
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking,
                    TimeSpan.FromSeconds(durationOfBreakSeconds)));
    }

    /// <summary>
    /// Add Auth Service HTTP client for API key validation
    /// </summary>
    public static IServiceCollection AddAuthServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthServiceOptions>(
            configuration.GetSection("Services:AuthService"));

        services.AddHttpClient<MoneyBee.Common.Abstractions.IApiKeyValidator, 
            MoneyBee.Common.Infrastructure.Caching.CachedApiKeyValidator>(
            (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<AuthServiceOptions>>().Value;
                client.BaseAddress = new Uri(options.Url);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        return services;
    }

    /// <summary>
    /// Apply database migrations automatically
    /// </summary>
    public static async Task<WebApplication> ApplyMigrationsAsync<TContext>(
        this WebApplication app) 
        where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        
        try
        {
            await db.Database.MigrateAsync();
            Log.Information("{Context} migrations applied successfully", typeof(TContext).Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error applying {Context} migrations", typeof(TContext).Name);
        }

        return app;
    }

    /// <summary>
    /// Configure standard middleware pipeline
    /// </summary>
    public static WebApplication UseStandardMiddleware(this WebApplication app)
    {
        // Global exception handling - must be first
        app.UseMiddleware<MoneyBee.Common.Middleware.GlobalExceptionHandlerMiddleware>();

        app.UseSerilogRequestLogging();

        // API Key Authentication
        app.UseMiddleware<MoneyBee.Common.Middleware.ApiKeyAuthenticationMiddleware>();

        return app;
    }
}
