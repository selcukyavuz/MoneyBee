using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.Services;
using MoneyBee.Customer.Service.Application.Interfaces;
using MoneyBee.Customer.Service.Domain.Interfaces;
using MoneyBee.Customer.Service.Infrastructure.Data;
using MoneyBee.Customer.Service.Infrastructure.ExternalServices;
using MoneyBee.Customer.Service.Infrastructure.Messaging;
using MoneyBee.Customer.Service.Infrastructure.Repositories;
using MoneyBee.Customer.Service.BackgroundServices;
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

// HTTP Clients
builder.Services.AddHttpClient("KycService");

// Clean Architecture - Dependency Injection
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, MoneyBee.Customer.Service.Application.Services.CustomerService>();

// DDD - Domain Services
builder.Services.AddScoped<MoneyBee.Customer.Service.Domain.Services.CustomerDomainService>();

// Infrastructure Services
builder.Services.AddScoped<IKycService, KycService>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// Background Services
builder.Services.AddHostedService<KycRetryService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database")
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

// Make Program accessible to integration tests
namespace MoneyBee.Customer.Service
{
    public partial class Program { }
}
