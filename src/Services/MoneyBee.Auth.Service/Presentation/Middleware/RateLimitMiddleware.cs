using Microsoft.Extensions.Options;
using MoneyBee.Auth.Service.Infrastructure;

namespace MoneyBee.Auth.Service.Presentation.Middleware;

public class RateLimitMiddleware(
    RequestDelegate next,
    ILogger<RateLimitMiddleware> logger,
    IOptions<RateLimitOptions> options)
{
    private readonly RateLimitOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
    {
        // Skip rate limiting for health check and internal validation endpoint
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        if (path.Contains(RateLimitConstants.HealthCheckPath) ||
            path.Contains(RateLimitConstants.InternalValidationPath))
        {
            await next(context);
            return;
        }

        // Use API Key ID or IP address as identifier
        var identifier = context.Items[RateLimitConstants.ApiKeyIdContextKey]?.ToString() 
                        ?? context.Connection.RemoteIpAddress?.ToString() 
                        ?? RateLimitConstants.UnknownIdentifier;

        var isAllowed = await rateLimitService.IsRequestAllowedAsync(identifier);

        if (!isAllowed)
        {
            var rateLimitInfo = await rateLimitService.GetRateLimitInfoAsync(identifier);
            
            logger.LogWarning(
                "Rate limit exceeded for {Identifier}. Limit: {Limit}, Reset: {ResetTime}, Path: {Path}",
                identifier, rateLimitInfo.Limit, rateLimitInfo.ResetTime, context.Request.Path);
            
            context.Response.StatusCode = _options.StatusCode;
            context.Response.Headers[RateLimitConstants.RateLimitLimitHeader] = rateLimitInfo.Limit.ToString();
            context.Response.Headers[RateLimitConstants.RateLimitRemainingHeader] = "0";
            context.Response.Headers[RateLimitConstants.RateLimitResetHeader] = rateLimitInfo.ResetTime.ToString("o");
            context.Response.Headers[RateLimitConstants.RetryAfterHeader] = _options.RetryAfterSeconds.ToString();

            await context.Response.WriteAsJsonAsync(new
            {
                error = _options.ErrorMessage,
                message = string.Format(_options.DetailMessage, _options.RequestLimit, _options.WindowInSeconds),
                retryAfter = _options.RetryAfterSeconds
            });
            return;
        }

        // Add rate limit headers to response
        var info = await rateLimitService.GetRateLimitInfoAsync(identifier);
        
        logger.LogDebug(
            "Request allowed for {Identifier}. Remaining: {Remaining}/{Limit}, Path: {Path}",
            identifier, info.Remaining, info.Limit, context.Request.Path);
        
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[RateLimitConstants.RateLimitLimitHeader] = info.Limit.ToString();
            context.Response.Headers[RateLimitConstants.RateLimitRemainingHeader] = info.Remaining.ToString();
            context.Response.Headers[RateLimitConstants.RateLimitResetHeader] = info.ResetTime.ToString("o");
            return Task.CompletedTask;
        });

        await next(context);
    }
}
