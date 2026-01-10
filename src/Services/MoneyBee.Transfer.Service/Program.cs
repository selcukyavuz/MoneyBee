using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.DDD;
using MoneyBee.Common.Persistence;
using MoneyBee.Common.Services;
using MoneyBee.Transfer.Service.Application.DomainEventHandlers;
using MoneyBee.Transfer.Service.Domain.Events;
using MoneyBee.Transfer.Service.Infrastructure.Caching;
using MoneyBee.Transfer.Service.Infrastructure.Data;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices;
using MoneyBee.Transfer.Service.Infrastructure.Messaging;
using MoneyBee.Transfer.Service.Infrastructure.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
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
        Name = "X-API-Key",
        Description = "API Key for authentication"
    });
});

// Database
builder.Services.AddDbContext<TransferDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString!));

// HTTP Clients
builder.Services.AddHttpClient("FraudService");
builder.Services.AddHttpClient("ExchangeRateService");
builder.Services.AddHttpClient("CustomerService");

// Services
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<MoneyBee.Transfer.Service.Domain.Interfaces.ITransferRepository, MoneyBee.Transfer.Service.Infrastructure.Repositories.TransferRepository>();
builder.Services.AddScoped<MoneyBee.Transfer.Service.Application.Interfaces.ITransferService, MoneyBee.Transfer.Service.Application.Services.TransferService>();

// Caching
builder.Services.AddScoped<ITransferCacheService, TransferCacheService>();

// Metrics
builder.Services.AddSingleton<TransferMetrics>();

// OpenTelemetry Metrics & Tracing
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "MoneyBee.Transfer.Service",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName))
    .WithMetrics(metrics => metrics
        .AddMeter("MoneyBee.Transfer.Service")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.SetDbStatementForStoredProcedure = true;
        })
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["Jaeger:Endpoint"] ?? "http://localhost:4317");
        }));

// DDD - Domain Services
builder.Services.AddScoped<MoneyBee.Transfer.Service.Domain.Services.TransferDomainService>();

// Infrastructure Services
builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
builder.Services.AddSingleton<MoneyBee.Transfer.Service.Infrastructure.Messaging.IEventPublisher, MoneyBee.Transfer.Service.Infrastructure.Messaging.RabbitMqEventPublisher>();

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<TransferDbContext>>();

// DDD - Domain Event Handlers
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<IDomainEventHandler<TransferCreatedDomainEvent>, TransferCreatedDomainEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<TransferCompletedDomainEvent>, TransferCompletedDomainEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<TransferCancelledDomainEvent>, TransferCancelledDomainEventHandler>();

// Background Services
builder.Services.AddHostedService<CustomerEventConsumer>();
builder.Services.AddHostedService<OutboxProcessor<TransferDbContext>>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database")
    .AddRedis(redisConnectionString!, "redis");

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

app.UseSerilogRequestLogging();

// OpenTelemetry Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

Log.Information("MoneyBee Transfer Service starting...");

app.Run();

// Make Program accessible to integration tests
namespace MoneyBee.Transfer.Service
{
    public partial class Program { }
}
