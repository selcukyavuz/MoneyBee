using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Auth.Service.Helpers;

namespace MoneyBee.Auth.Service.Middleware;

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

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        // Skip authentication for health check, metrics, and swagger endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path == "/health" || 
            path == "/metrics" ||
            path.StartsWith("/swagger") || 
            (path == "/api/apikeys" && context.Request.Method == "POST")) // Allow creating first API key
        {
            await _next(context);
            return;
        }

        // Extract API Key from header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is missing" });
            return;
        }

        var apiKey = extractedApiKey.ToString();

        // Validate API Key format
        if (!ApiKeyHelper.IsValidApiKeyFormat(apiKey))
        {
            _logger.LogWarning("Invalid API Key format from {IpAddress}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key format" });
            return;
        }

        // Validate API Key using service
        var isValid = await apiKeyService.ValidateApiKeyAsync(apiKey);

        if (!isValid)
        {
            _logger.LogWarning("Invalid API Key attempt from {IpAddress}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired API Key" });
            return;
        }

        // Update last used timestamp (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await apiKeyService.UpdateLastUsedAsync(apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update LastUsedAt for API Key");
            }
        });

        await _next(context);
    }
}
