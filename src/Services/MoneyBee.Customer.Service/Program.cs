using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.DDD;
using MoneyBee.Common.Persistence;
using MoneyBee.Common.Services;
using MoneyBee.Customer.Service.Application.DomainEventHandlers;
using MoneyBee.Customer.Service.Application.Interfaces;
using MoneyBee.Customer.Service.Domain.Events;
using MoneyBee.Customer.Service.Domain.Interfaces;
using MoneyBee.Customer.Service.Infrastructure.Data;
using MoneyBee.Customer.Service.Infrastructure.ExternalServices;
using MoneyBee.Customer.Service.Infrastructure.Messaging;
using MoneyBee.Customer.Service.Infrastructure.Repositories;
using MoneyBee.Customer.Service.BackgroundServices;
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
        Title = "MoneyBee Customer Service API", 
        Version = "v1",
        Description = "Customer Management and KYC Verification Service for MoneyBee Money Transfer System"
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
builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString!));

// HTTP Clients
builder.Services.AddHttpClient("KycService");

// Clean Architecture - Dependency Injection
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, MoneyBee.Customer.Service.Application.Services.CustomerService>();

// DDD - Domain Services
builder.Services.AddScoped<MoneyBee.Customer.Service.Domain.Services.CustomerDomainService>();

// DDD - Domain Event Handlers
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<IDomainEventHandler<CustomerCreatedDomainEvent>, CustomerCreatedDomainEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<CustomerDeletedDomainEvent>, CustomerDeletedDomainEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<CustomerStatusChangedDomainEvent>, CustomerStatusChangedDomainEventHandler>();

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<CustomerDbContext>>();

// Infrastructure Services
builder.Services.AddScoped<IKycService, KycService>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// Background Services
builder.Services.AddHostedService<KycRetryService>();
builder.Services.AddHostedService<OutboxProcessor<CustomerDbContext>>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database")
    .AddRedis(redisConnectionString!, "redis")
    .AddRabbitMQ(rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMQ:Username"]}:{builder.Configuration["RabbitMQ:Password"]}@{builder.Configuration["RabbitMQ:Host"]}", name: "rabbitmq");

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

app.UseSerilogRequestLogging();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

Log.Information("MoneyBee Customer Service starting...");

app.Run();
