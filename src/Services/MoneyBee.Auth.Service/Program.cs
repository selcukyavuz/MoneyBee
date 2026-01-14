using Microsoft.EntityFrameworkCore;
using MoneyBee.Auth.Service.Domain.ApiKeys;
using MoneyBee.Auth.Service.Presentation.ApiKeys;
using MoneyBee.Auth.Service.Presentation.Middleware;
using MoneyBee.Auth.Service.Infrastructure.Data;
using MoneyBee.Auth.Service.Infrastructure.ApiKeys;
using MoneyBee.Auth.Service.Infrastructure;
using MoneyBee.Common.Abstractions;
using MoneyBee.Web.Common.Extensions;
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
builder.Services.AddSwaggerWithApiKey(
    "MoneyBee Auth Service API",
    "Authentication and Rate Limiting Service for MoneyBee Money Transfer System");

// Database
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MoneyBee.Auth.Service")));

// Redis
builder.Services.AddRedisConnectionMultiplexer(builder.Configuration);

// Options Pattern
builder.Services.Configure<MoneyBee.Auth.Service.Infrastructure.RateLimitOptions>(
    builder.Configuration.GetSection(MoneyBee.Auth.Service.Infrastructure.RateLimitOptions.SectionName));

// Clean Architecture - Dependency Injection
var authAssembly = typeof(Program).Assembly;

// Automatic registration using Scrutor
builder.Services.AddApplicationHandlers(authAssembly);
builder.Services.AddRepositories(authAssembly);
builder.Services.AddInfrastructureServices(authAssembly);

// API Key Validator (direct DB access for Auth Service)
builder.Services.AddScoped<IApiKeyValidator, DirectApiKeyValidator>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database");

var app = builder.Build();

// Run migrations automatically
await app.ApplyMigrationsAsync<AuthDbContext>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Standard middleware pipeline
app.UseStandardMiddleware();

// Rate limiting middleware (specific to Auth Service)
app.UseMiddleware<RateLimitMiddleware>();

// Map endpoints
app.MapApiKeyEndpoints();

// Health check endpoint
app.MapHealthChecks("/health");

Log.Information("MoneyBee Auth Service starting...");

app.Run();

// Make Program accessible to integration tests
namespace MoneyBee.Auth.Service
{
    public partial class Program { }
}
