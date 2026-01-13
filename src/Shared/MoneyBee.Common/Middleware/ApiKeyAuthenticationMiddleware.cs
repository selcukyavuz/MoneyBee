using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MoneyBee.Common.Constants;
using System.Text.Json;

namespace MoneyBee.Common.Middleware;

/// <summary>
/// Middleware for API key authentication across all services
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, Services.IApiKeyValidator apiKeyValidator)
    {
        // Skip authentication for health check, metrics, and swagger endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path == "/health" || 
            path == "/metrics" ||
            path.StartsWith("/swagger") || 
            (path.Contains("/api/auth/keys") && context.Request.Method == "POST") || // Allow creating API key in Auth Service
            (path.Contains("/apikeys") && context.Request.Method == "POST")) // Allow creating first API key
        {
            await _next(context);
            return;
        }

        // Extract API Key from header
        if (!context.Request.Headers.TryGetValue(HttpHeaders.ApiKey, out var extractedApiKey))
        {
            _logger.LogWarning("API Key missing from request to {Path} from {IpAddress}", 
                context.Request.Path, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "API Key is missing" }));
            return;
        }

        var apiKey = extractedApiKey.ToString();

        // Validate API Key format (must start with mb_ and have reasonable length)
        if (string.IsNullOrWhiteSpace(apiKey) || 
            !apiKey.StartsWith("mb_") || 
            apiKey.Length < 20)
        {
            _logger.LogWarning("Invalid API Key format from {IpAddress}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Invalid API Key format" }));
            return;
        }

        // Validate API Key using validator (with cache)
        var isValid = await apiKeyValidator.ValidateApiKeyAsync(apiKey);

        if (!isValid)
        {
            _logger.LogWarning("Invalid API Key attempt from {IpAddress} for path {Path}", 
                context.Connection.RemoteIpAddress, context.Request.Path);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Invalid or expired API Key" }));
            return;
        }

        _logger.LogDebug("API Key validated successfully for path {Path}", context.Request.Path);
        await _next(context);
    }
}
