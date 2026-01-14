using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoneyBee.Common.Constants;
using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Infrastructure.Locking;
using MoneyBee.Common.Options;
using MoneyBee.Transfer.Service.Application.Transfers.Options;
using MoneyBee.Transfer.Service.Application.Transfers;
using MoneyBee.Transfer.Service.Application.Transfers.Services;
using MoneyBee.Transfer.Service.Application.Transfers.Commands.CreateTransfer;
using MoneyBee.Transfer.Service.Application.Transfers.Commands.CancelTransfer;
using MoneyBee.Transfer.Service.Application.Transfers.Commands.CompleteTransfer;
using MoneyBee.Transfer.Service.Application.Transfers.Queries.GetTransferByCode;
using MoneyBee.Transfer.Service.Application.Transfers.Queries.GetCustomerTransfers;
using MoneyBee.Transfer.Service.Application.Transfers.Queries.CheckDailyLimit;
using MoneyBee.Transfer.Service.Presentation;
using MoneyBee.Transfer.Service.Infrastructure.Data;
using MoneyBee.Transfer.Service.Infrastructure.Messaging;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.CustomerService;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.ExchangeRateService;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.FraudDetectionService;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using Serilog;
using StackExchange.Redis;

// PostgreSQL timestamp compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Options
builder.Services.Configure<TransferSettings>(builder.Configuration.GetSection("TransferSettings"));
builder.Services.Configure<FeeSettings>(builder.Configuration.GetSection("FeeSettings"));
builder.Services.Configure<CustomerServiceOptions>(builder.Configuration.GetSection(ConfigurationKeys.ExternalServices.CustomerService));
builder.Services.Configure<FraudDetectionServiceOptions>(builder.Configuration.GetSection(ConfigurationKeys.ExternalServices.FraudService));
builder.Services.Configure<ExchangeRateServiceOptions>(builder.Configuration.GetSection(ConfigurationKeys.ExternalServices.ExchangeRateService));
builder.Services.Configure<AuthServiceOptions>(
    builder.Configuration.GetSection("Services:AuthService"));
builder.Services.Configure<MoneyBee.Common.Options.RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMQ"));

// Add services to the container
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "MoneyBee Transfer Service API", 
        Version = "v1",
        Description = "Money Transfer Management Service for MoneyBee Money Transfer System"
    });
    c.AddSecurityDefinition("ApiKey", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = HttpHeaders.ApiKey,
        Description = "API Key for authentication"
    });
});

// Database
builder.Services.AddDbContext<TransferDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// HTTP Clients with Typed Client Pattern
builder.Services.AddHttpClient<ICustomerService, CustomerService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<CustomerServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

builder.Services.AddHttpClient<IFraudDetectionService, FraudDetectionService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<FraudDetectionServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ExchangeRateServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Auth Service HTTP Client for API key validation
builder.Services.AddHttpClient<MoneyBee.Common.Abstractions.IApiKeyValidator, MoneyBee.Common.Infrastructure.Caching.CachedApiKeyValidator>(
    (sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<AuthServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.Url);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    });

// Services
builder.Services.AddScoped<MoneyBee.Transfer.Service.Domain.Transfers.ITransferRepository, MoneyBee.Transfer.Service.Infrastructure.Transfers.TransferRepository>();
builder.Services.AddSingleton<MoneyBee.Transfer.Service.Application.Transfers.Services.ITransactionCodeGenerator, MoneyBee.Transfer.Service.Infrastructure.Transfers.Services.TransactionCodeGenerator>();

// Command Handlers
builder.Services.AddScoped<CreateTransferHandler>();
builder.Services.AddScoped<CompleteTransferHandler>();
builder.Services.AddScoped<CancelTransferHandler>();

// Query Handlers  
builder.Services.AddScoped<GetTransferByCodeHandler>();
builder.Services.AddScoped<GetCustomerTransfersHandler>();
builder.Services.AddScoped<CheckDailyLimitHandler>();

// Redis
var redisConfig = ConfigurationOptions.Parse(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379");
redisConfig.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConfig));

// Redis Cache for API Key validation
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    options.InstanceName = "TransferService:";
});

// RabbitMQ
builder.Services.AddSingleton<IConnection>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var options = sp.GetRequiredService<IOptions<MoneyBee.Common.Options.RabbitMqOptions>>().Value;
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
        logger.LogInformation("RabbitMQ connection established");
        return connection;
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to connect to RabbitMQ. Service will start without message queue support.");
        return null!;
    }
});

// Infrastructure Services
builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
builder.Services.AddSingleton<MoneyBee.Common.Abstractions.IEventPublisher>(sp =>
{
    var connection = sp.GetRequiredService<IConnection?>();
    var logger = sp.GetRequiredService<ILogger<MoneyBee.Common.Infrastructure.Messaging.RabbitMqEventPublisher>>();
    
    Func<string, string> routingKeyResolver = eventTypeName => eventTypeName switch
    {
        "TransferCreatedEvent" => "transfer.created",
        "TransferCompletedEvent" => "transfer.completed",
        "TransferCancelledEvent" => "transfer.cancelled",
        _ => $"transfer.{eventTypeName.ToLower()}"
    };
    
    return new MoneyBee.Common.Infrastructure.Messaging.RabbitMqEventPublisher(connection, logger, routingKeyResolver, "Transfer Service");
});

// Background Services
builder.Services.AddHostedService<CustomerEventConsumer>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database")
    .AddRabbitMQ(name: "rabbitmq");

var app = builder.Build();

// Run migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TransferDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error applying database migrations");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handling - must be first
app.UseMiddleware<MoneyBee.Common.Middleware.GlobalExceptionHandlerMiddleware>();

app.UseSerilogRequestLogging();

// API Key Authentication
app.UseMiddleware<MoneyBee.Common.Middleware.ApiKeyAuthenticationMiddleware>();

// Map endpoints
app.MapTransferEndpoints();

// Health check endpoint
app.MapHealthChecks("/health");

Log.Information("MoneyBee Transfer Service starting...");

app.Run();

// Make Program accessible to integration tests
namespace MoneyBee.Transfer.Service
{
    public partial class Program { }
}
