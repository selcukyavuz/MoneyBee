namespace MoneyBee.Auth.Service.Presentation.Middleware;

/// <summary>
/// Constants for rate limiting middleware
/// </summary>
public static class RateLimitConstants
{
    // Headers
    public const string RateLimitLimitHeader = "X-RateLimit-Limit";
    public const string RateLimitRemainingHeader = "X-RateLimit-Remaining";
    public const string RateLimitResetHeader = "X-RateLimit-Reset";
    public const string RetryAfterHeader = "Retry-After";

    // Paths
    public const string HealthCheckPath = "/health";
    public const string InternalValidationPath = "/api/v1/apikeys/validate";

    // Context items
    public const string ApiKeyIdContextKey = "ApiKeyId";
    public const string UnknownIdentifier = "unknown";
}
