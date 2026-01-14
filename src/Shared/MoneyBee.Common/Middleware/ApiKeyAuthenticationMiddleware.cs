using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Constants;
using System.Text.Json;

namespace MoneyBee.Common.Middleware;

/// <summary>
/// Middleware for API key authentication across all services
/// </summary>
public class ApiKeyAuthenticationMiddleware(
    RequestDelegate next,
    ILogger<ApiKeyAuthenticationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IApiKeyValidator apiKeyValidator)
    {
        // Skip authentication for health check, and swagger endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path == ApiKeyAuthenticationMiddlewareConstants.HealthCheckPath || 
            path.StartsWith(ApiKeyAuthenticationMiddlewareConstants.SwaggerPathPrefix) || 
            (path.Contains(ApiKeyAuthenticationMiddlewareConstants.AuthServiceKeysPath) && context.Request.Method == ApiKeyAuthenticationMiddlewareConstants.PostMethod) ||
            (path.Contains(ApiKeyAuthenticationMiddlewareConstants.ApiKeysPath) && context.Request.Method == ApiKeyAuthenticationMiddlewareConstants.PostMethod))
        {
            await next(context);
            return;
        }

        // Extract API Key from header
        if (!context.Request.Headers.TryGetValue(HttpHeaders.ApiKey, out var extractedApiKey))
        {
            logger.LogWarning(ApiKeyAuthenticationMiddlewareConstants.ApiKeyMissingLogMessage, 
                context.Request.Path, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            context.Response.ContentType = ApiKeyAuthenticationMiddlewareConstants.JsonContentType;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ApiKeyAuthenticationMiddlewareConstants.ApiKeyMissingError }));
            return;
        }

        var apiKey = extractedApiKey.ToString();

        // Validate API Key format (must start with mb_ and have reasonable length)
        if (string.IsNullOrWhiteSpace(apiKey) || 
            !apiKey.StartsWith(ApiKeyAuthenticationMiddlewareConstants.ApiKeyPrefix) || 
            apiKey.Length < ApiKeyAuthenticationMiddlewareConstants.MinApiKeyLength)
        {
            logger.LogWarning(ApiKeyAuthenticationMiddlewareConstants.InvalidFormatLogMessage, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            context.Response.ContentType = ApiKeyAuthenticationMiddlewareConstants.JsonContentType;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ApiKeyAuthenticationMiddlewareConstants.InvalidApiKeyFormatError }));
            return;
        }

        // Validate API Key using validator (with cache)
        var isValid = await apiKeyValidator.ValidateApiKeyAsync(apiKey);

        if (!isValid)
        {
            logger.LogWarning(ApiKeyAuthenticationMiddlewareConstants.InvalidAttemptLogMessage, 
                context.Connection.RemoteIpAddress, context.Request.Path);
            context.Response.StatusCode = 401;
            context.Response.ContentType = ApiKeyAuthenticationMiddlewareConstants.JsonContentType;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ApiKeyAuthenticationMiddlewareConstants.InvalidOrExpiredApiKeyError }));
            return;
        }

        logger.LogDebug(ApiKeyAuthenticationMiddlewareConstants.ValidatedSuccessfullyLogMessage, context.Request.Path);
        await next(context);
    }
}
