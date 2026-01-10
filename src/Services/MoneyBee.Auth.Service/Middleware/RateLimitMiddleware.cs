using MoneyBee.Auth.Service.Services;

namespace MoneyBee.Auth.Service.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
    {
        // Skip rate limiting for health check
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/health"))
        {
            await _next(context);
            return;
        }

        // Use API Key ID or IP address as identifier
        var identifier = context.Items["ApiKeyId"]?.ToString() 
                        ?? context.Connection.RemoteIpAddress?.ToString() 
                        ?? "unknown";

        var isAllowed = await rateLimitService.IsRequestAllowedAsync(identifier);

        if (!isAllowed)
        {
            var rateLimitInfo = await rateLimitService.GetRateLimitInfoAsync(identifier);
            
            context.Response.StatusCode = 429;
            context.Response.Headers["X-RateLimit-Limit"] = rateLimitInfo.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["X-RateLimit-Reset"] = rateLimitInfo.ResetTime.ToString("o");
            context.Response.Headers["Retry-After"] = "60";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = "Maximum 100 requests per minute allowed",
                retryAfter = 60
            });
            return;
        }

        // Add rate limit headers to response
        var info = await rateLimitService.GetRateLimitInfoAsync(identifier);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Limit"] = info.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = info.Remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = info.ResetTime.ToString("o");
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
