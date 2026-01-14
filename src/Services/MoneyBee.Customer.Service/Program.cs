using Microsoft.EntityFrameworkCore;
using MoneyBee.Customer.Service.Domain.Services;
using MoneyBee.Customer.Service.Presentation.Customers;
using MoneyBee.Customer.Service.Infrastructure.Data;
using MoneyBee.Customer.Service.Infrastructure.ExternalServices;
using MoneyBee.Customer.Service.Infrastructure.HostedServices;
using MoneyBee.Common.Abstractions;
using MoneyBee.Web.Common.Extensions;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using Serilog;
using System.Text.Json.Serialization;
using MoneyBee.Customer.Service.Application.Customers.Commands.CreateCustomer;
using MoneyBee.Customer.Service.Application.Customers.Commands.UpdateCustomer;
using MoneyBee.Customer.Service.Application.Customers.Commands.UpdateCustomerStatus;
using MoneyBee.Customer.Service.Application.Customers.Commands.DeleteCustomer;
using MoneyBee.Customer.Service.Application.Customers.Queries.VerifyCustomer;
using MoneyBee.Customer.Service.Application.Customers.Queries.GetCustomerById;
using MoneyBee.Customer.Service.Application.Customers.Queries.GetAllCustomers;
using MoneyBee.Customer.Service.Application.Customers.Queries.GetCustomerByNationalId;
using MoneyBee.Common.Infrastructure.Messaging;

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

// Repositories
builder.Services.AddScoped<MoneyBee.Customer.Service.Domain.Customers.ICustomerRepository, 
    MoneyBee.Customer.Service.Infrastructure.Customers.CustomerRepository>();

// Handlers
builder.Services.AddScoped<CreateCustomerHandler>();
builder.Services.AddScoped<UpdateCustomerHandler>();
builder.Services.AddScoped<UpdateCustomerStatusHandler>();
builder.Services.AddScoped<DeleteCustomerHandler>();
builder.Services.AddScoped<VerifyCustomerHandler>();
builder.Services.AddScoped<GetCustomerByIdHandler>();
builder.Services.AddScoped<GetAllCustomersHandler>();
builder.Services.AddScoped<GetCustomerByNationalIdHandler>();

// External Services - KycService with Named HttpClient
var kycHttpClientBuilder = builder.Services.AddHttpClient(nameof(KycService))
    .ConfigureHttpClient((sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var kycServiceUrl = configuration[MoneyBee.Common.Constants.ConfigurationKeys.ExternalServices.KycService] 
            ?? "http://kyc-service";
        client.BaseAddress = new Uri(kycServiceUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30)));

builder.Services.AddScoped<IKycService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient(nameof(KycService));
    var logger = sp.GetRequiredService<ILogger<KycService>>();
    return new KycService(httpClient, logger);
});

// RabbitMQ
builder.Services.AddRabbitMq(builder.Configuration);

builder.Services.AddSingleton<IEventPublisher>(sp =>
{
    var connection = sp.GetService<IConnection>();
    var logger = sp.GetRequiredService<ILogger<RabbitMqEventPublisher>>();
    
    Func<string, string> routingKeyResolver = eventTypeName => eventTypeName switch
    {
        "CustomerStatusChangedEvent" => "customer.status.changed",
        "CustomerCreatedEvent" => "customer.created",
        "CustomerDeletedEvent" => "customer.deleted",
        _ => $"customer.{eventTypeName.ToLower()}"
    };
    
    return new RabbitMqEventPublisher(connection, logger, routingKeyResolver, "Customer Service");
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
