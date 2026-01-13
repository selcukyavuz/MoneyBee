using Microsoft.EntityFrameworkCore;
using MoneyBee.Customer.Service.Application.Interfaces;
using MoneyBee.Customer.Service.Domain.Interfaces;
using MoneyBee.Customer.Service.Endpoints;
using MoneyBee.Customer.Service.Infrastructure.Data;
using MoneyBee.Customer.Service.Infrastructure.ExternalServices;
using MoneyBee.Customer.Service.Infrastructure.Messaging;
using MoneyBee.Customer.Service.Infrastructure.Repositories;
using MoneyBee.Customer.Service.BackgroundServices;
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

// Add services to the container
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "MoneyBee Customer Service API", 
        Version = "v1",
        Description = "Customer Management and KYC Verification Service for MoneyBee Money Transfer System"
    });
    c.AddSecurityDefinition("ApiKey", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = MoneyBee.Common.Constants.HttpHeaders.ApiKey,
        Description = "API Key for authentication"
    });
});

// Database
builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis Cache for API Key validation
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "CustomerService:";
});

// HTTP Clients
builder.Services.AddHttpClient("KycService");

// Auth Service HTTP Client for API key validation
builder.Services.AddHttpClient<MoneyBee.Common.Services.IApiKeyValidator, MoneyBee.Common.Services.CachedApiKeyValidator>(client =>
{
    var authServiceUrl = builder.Configuration["Services:AuthService:Url"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(authServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

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

// Clean Architecture - Dependency Injection
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, MoneyBee.Customer.Service.Application.Services.CustomerService>();

// Infrastructure Services
builder.Services.AddScoped<IKycService, KycService>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// Background Services
builder.Services.AddHostedService<KycRetryService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database")
    .AddRabbitMQ(name: "rabbitmq");

var app = builder.Build();

// Run migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
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
app.MapCustomerEndpoints();

// Health check endpoint
app.MapHealthChecks("/health");

Log.Information("MoneyBee Customer Service starting...");

app.Run();

// Make Program accessible to integration tests
namespace MoneyBee.Customer.Service
{
    public partial class Program { }
}
