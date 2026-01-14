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
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MoneyBee.Customer.Service")));

// Configure Auth Service options
builder.Services.Configure<AuthServiceOptions>(
    builder.Configuration.GetSection("Services:AuthService"));

// Configure RabbitMQ options
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMQ"));

// Redis Cache for API Key validation
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "CustomerService:";
});

// HTTP Clients
builder.Services.AddHttpClient<IKycService, KycService>(client =>
{
    var kycServiceUrl = builder.Configuration[MoneyBee.Common.Constants.ConfigurationKeys.ExternalServices.KycService] ?? "http://kyc-service";
    client.BaseAddress = new Uri(kycServiceUrl);
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)));

// Auth Service HTTP Client for API key validation
builder.Services.AddHttpClient<MoneyBee.Common.Abstractions.IApiKeyValidator, MoneyBee.Common.Infrastructure.Caching.CachedApiKeyValidator>(
    (sp, client) =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.Url);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    });

// RabbitMQ
builder.Services.AddSingleton<IConnection>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;
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

// Clean Architecture - Dependency Injection
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

// Command Handlers
builder.Services.AddScoped<MoneyBee.Customer.Service.Application.Customers.Commands.CreateCustomer.CreateCustomerHandler>();
builder.Services.AddScoped<MoneyBee.Customer.Service.Application.Customers.Commands.UpdateCustomer.UpdateCustomerHandler>();
builder.Services.AddScoped<MoneyBee.Customer.Service.Application.Customers.Commands.UpdateCustomerStatus.UpdateCustomerStatusHandler>();
builder.Services.AddScoped<MoneyBee.Customer.Service.Application.Customers.Commands.DeleteCustomer.DeleteCustomerHandler>();

// Query Handlers
builder.Services.AddScoped<MoneyBee.Customer.Service.Application.Customers.Queries.GetCustomerById.GetCustomerByIdHandler>();
builder.Services.AddScoped<MoneyBee.Customer.Service.Application.Customers.Queries.GetCustomerByNationalId.GetCustomerByNationalIdHandler>();
builder.Services.AddScoped<MoneyBee.Customer.Service.Application.Customers.Queries.GetAllCustomers.GetAllCustomersHandler>();
builder.Services.AddScoped<MoneyBee.Customer.Service.Application.Customers.Queries.VerifyCustomer.VerifyCustomerHandler>();

// Infrastructure Services
builder.Services.AddScoped<IKycService, KycService>();
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
