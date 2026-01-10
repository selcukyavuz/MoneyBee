using Microsoft.EntityFrameworkCore;
using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Auth.Service.Application.Services;
using MoneyBee.Auth.Service.Application.Validators;
using MoneyBee.Auth.Service.Domain.Interfaces;
using MoneyBee.Auth.Service.Infrastructure.Data;
using MoneyBee.Auth.Service.Infrastructure.Repositories;
using MoneyBee.Auth.Service.Middleware;
using MoneyBee.Auth.Service.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
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
        Title = "MoneyBee Auth Service API", 
        Version = "v1",
        Description = "Authentication and Rate Limiting Service for MoneyBee Money Transfer System"
    });
    c.AddSecurityDefinition("ApiKey", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key for authentication"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString!));

// Clean Architecture - Dependency Injection
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IRateLimitService, RateLimitService>();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateApiKeyValidator>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database")
    .AddRedis(redisConnectionString!, "redis");

var app = builder.Build();

// Run migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
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

// Custom Middleware
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

Log.Information("MoneyBee Auth Service starting...");

app.Run();
