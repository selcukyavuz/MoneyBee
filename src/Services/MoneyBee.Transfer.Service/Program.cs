using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoneyBee.Common.Constants;
using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Infrastructure.Locking;
using MoneyBee.Common.Options;
using MoneyBee.Transfer.Service.Application.Transfers.Options;
using MoneyBee.Transfer.Service.Application.Transfers.Services;
using MoneyBee.Transfer.Service.Presentation;
using MoneyBee.Transfer.Service.Infrastructure.Data;
using MoneyBee.Transfer.Service.Infrastructure.Messaging;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.CustomerService;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.ExchangeRateService;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.FraudDetectionService;
using MoneyBee.Web.Common.Extensions;
using RabbitMQ.Client;
using Serilog;

// PostgreSQL timestamp compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Options
builder.Services.Configure<TransferSettings>(builder.Configuration.GetSection(ConfigurationKeys.Fee.SectionName));
builder.Services.Configure<FeeSettings>(builder.Configuration.GetSection(ConfigurationKeys.Fee.SectionName));
builder.Services.Configure<CustomerServiceOptions>(builder.Configuration.GetSection(ConfigurationKeys.ExternalServices.CustomerService));
builder.Services.Configure<FraudDetectionServiceOptions>(builder.Configuration.GetSection(ConfigurationKeys.ExternalServices.FraudService));
builder.Services.Configure<ExchangeRateServiceOptions>(builder.Configuration.GetSection(ConfigurationKeys.ExternalServices.ExchangeRateService));
builder.Services.Configure<AuthServiceOptions>(builder.Configuration.GetSection(ConfigurationKeys.Services.AuthService));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(ConfigurationKeys.RabbitMQ.SectionName));
// Add services to the container
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithApiKey(
    "MoneyBee Transfer Service API",
    "Money Transfer Management Service for MoneyBee Money Transfer System");

// Database
builder.Services.AddDbContext<TransferDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// HTTP Clients with Typed Client Pattern
builder.Services.AddHttpClientWithRetry<ICustomerService, CustomerService>(
    (sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["ExternalServices:CustomerService:BaseUrl"];
        var timeout = configuration.GetValue<int>("ExternalServices:CustomerService:TimeoutSeconds", 10);
        
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException("CustomerService BaseUrl is not configured");
        
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(timeout);
    },
    retryCount: 3);

builder.Services.AddHttpClientWithCircuitBreaker<IFraudDetectionService, FraudDetectionService>(
    (sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["ExternalServices:FraudService:BaseUrl"];
        var timeout = configuration.GetValue<int>("ExternalServices:FraudService:TimeoutSeconds", 10);
        
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException("FraudDetectionService BaseUrl is not configured");
        
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(timeout);
    },
    handledEventsAllowedBeforeBreaking: 5,
    durationOfBreakSeconds: 30);

builder.Services.AddHttpClientWithRetry<IExchangeRateService, ExchangeRateService>(
    (sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["ExternalServices:ExchangeRateService:BaseUrl"];
        var timeout = configuration.GetValue<int>("ExternalServices:ExchangeRateService:TimeoutSeconds", 10);
        
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException("ExchangeRateService BaseUrl is not configured");
        
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(timeout);
    },
    retryCount: 3);

// Auth Service for API key validation
builder.Services.AddAuthServiceClient(builder.Configuration);

// Clean Architecture - Dependency Injection
var transferAssembly = typeof(Program).Assembly;

// Automatic registration using Scrutor
builder.Services.AddApplicationHandlers(transferAssembly);
builder.Services.AddRepositories(transferAssembly);
builder.Services.AddInfrastructureServices(transferAssembly);

// Singleton Services
builder.Services.AddSingleton<ITransactionCodeGenerator, 
    MoneyBee.Transfer.Service.Infrastructure.Transfers.Services.TransactionCodeGenerator>();

// Redis
builder.Services.AddRedisConnectionMultiplexer(builder.Configuration);

// Redis Cache for API Key validation
builder.Services.AddRedisCacheWithInstance(builder.Configuration, "TransferService");

// RabbitMQ
builder.Services.AddRabbitMq(builder.Configuration);

// Infrastructure Services
builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
builder.Services.AddSingleton<MoneyBee.Common.Abstractions.IEventPublisher>(sp =>
{
    var connection = sp.GetService<IConnection>();
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
await app.ApplyMigrationsAsync<TransferDbContext>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Standard middleware pipeline
app.UseStandardMiddleware();

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
