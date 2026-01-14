using Microsoft.EntityFrameworkCore;
using MoneyBee.Customer.Service.Application.Customers;
using MoneyBee.Customer.Service.Domain.Customers;
using MoneyBee.Customer.Service.Domain.Services;
using MoneyBee.Customer.Service.Presentation.Customers;
using MoneyBee.Customer.Service.Infrastructure.Data;
using MoneyBee.Customer.Service.Infrastructure.ExternalServices;
using MoneyBee.Customer.Service.Infrastructure.Customers;
using MoneyBee.Customer.Service.Infrastructure.HostedServices;
using MoneyBee.Common.Options;
using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Infrastructure.Messaging;
using MoneyBee.Web.Common.Extensions;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using Serilog;
using System.Text.Json.Serialization;

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
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithApiKey(
    "MoneyBee Customer Service API",
    "Customer Management and KYC Verification Service for MoneyBee Money Transfer System");

// Database
builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MoneyBee.Customer.Service")));

// Auth Service for API key validation
builder.Services.AddAuthServiceClient(builder.Configuration);

// Redis Cache for API Key validation
builder.Services.AddRedisCacheWithInstance(builder.Configuration, "CustomerService");

// HTTP Clients
builder.Services.AddHttpClientWithCircuitBreaker<IKycService, KycService>(
    (sp, client) =>
    {
        var kycServiceUrl = builder.Configuration[MoneyBee.Common.Constants.ConfigurationKeys.ExternalServices.KycService] 
            ?? "http://kyc-service";
        client.BaseAddress = new Uri(kycServiceUrl);
    },
    handledEventsAllowedBeforeBreaking: 3,
    durationOfBreakSeconds: 30);

// RabbitMQ
builder.Services.AddRabbitMq(builder.Configuration);

// Clean Architecture - Dependency Injection
var customerAssembly = typeof(Program).Assembly;

// Automatic registration using Scrutor
builder.Services.AddApplicationHandlers(customerAssembly);
builder.Services.AddRepositories(customerAssembly);
builder.Services.AddInfrastructureServices(customerAssembly);
builder.Services.AddSingleton<IEventPublisher>(sp =>
{
    var connection = sp.GetService<IConnection>();
    var logger = sp.GetRequiredService<ILogger<MoneyBee.Common.Infrastructure.Messaging.RabbitMqEventPublisher>>();
    
    Func<string, string> routingKeyResolver = eventTypeName => eventTypeName switch
    {
        "CustomerStatusChangedEvent" => "customer.status.changed",
        "CustomerCreatedEvent" => "customer.created",
        "CustomerDeletedEvent" => "customer.deleted",
        _ => $"customer.{eventTypeName.ToLower()}"
    };
    
    return new MoneyBee.Common.Infrastructure.Messaging.RabbitMqEventPublisher(connection, logger, routingKeyResolver, "Customer Service");
});

// Background Services
builder.Services.AddHostedService<KycRetryService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database")
    .AddRabbitMQ(name: "rabbitmq");

var app = builder.Build();

// Run migrations automatically
await app.ApplyMigrationsAsync<CustomerDbContext>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Standard middleware pipeline
app.UseStandardMiddleware();

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
