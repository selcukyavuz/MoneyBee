using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.Services;
using MoneyBee.Transfer.Service.Endpoints;
using MoneyBee.Transfer.Service.Infrastructure.Data;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices;
using MoneyBee.Transfer.Service.Infrastructure.Messaging;
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
        Name = "X-API-Key",
        Description = "API Key for authentication"
    });
});

// Database
builder.Services.AddDbContext<TransferDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Redis
var redisConfig = ConfigurationOptions.Parse(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379");
redisConfig.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConfig));

// RabbitMQ
builder.Services.AddSingleton<IConnection>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    try
    {
        var factory = new ConnectionFactory()
        {
            HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = builder.Configuration["RabbitMQ:Username"] ?? "moneybee",
            Password = builder.Configuration["RabbitMQ:Password"] ?? "moneybee123",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
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
builder.Services.AddSingleton<MoneyBee.Transfer.Service.Infrastructure.Messaging.IEventPublisher, MoneyBee.Transfer.Service.Infrastructure.Messaging.RabbitMqEventPublisher>();

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

app.UseSerilogRequestLogging();

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
