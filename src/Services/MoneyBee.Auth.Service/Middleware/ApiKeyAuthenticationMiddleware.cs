using MoneyBee.Auth.Service.Data;
using MoneyBee.Auth.Service.Helpers;
using Microsoft.EntityFrameworkCore;

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

    public async Task InvokeAsync(HttpContext context, AuthDbContext dbContext)
    {
        // Skip authentication for health check and swagger endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/health") || 
            path.Contains("/swagger") || 
            path.Contains("/api/auth/keys") && context.Request.Method == "POST") // Allow creating first API key
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

        // Hash and validate against database
        var keyHash = ApiKeyHelper.HashApiKey(apiKey);
        var apiKeyEntity = await dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash);

        if (apiKeyEntity == null)
        {
            _logger.LogWarning("Invalid API Key attempt from {IpAddress}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
            return;
        }

        // Check if key is active
        if (!apiKeyEntity.IsActive)
        {
            _logger.LogWarning("Inactive API Key used: {KeyId}", apiKeyEntity.Id);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is inactive" });
            return;
        }

        // Check if key is expired
        if (apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired API Key used: {KeyId}", apiKeyEntity.Id);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key has expired" });
            return;
        }

        // Update last used timestamp (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                apiKeyEntity.LastUsedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update LastUsedAt for API Key {KeyId}", apiKeyEntity.Id);
            }
        });

        // Add API Key ID to context for logging/tracking
        context.Items["ApiKeyId"] = apiKeyEntity.Id;
        context.Items["ApiKeyName"] = apiKeyEntity.Name;

        await _next(context);
    }
}
