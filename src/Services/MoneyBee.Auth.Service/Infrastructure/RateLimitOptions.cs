namespace MoneyBee.Auth.Service.Infrastructure;

/// <summary>
/// Configuration options for rate limiting
/// </summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    /// <summary>
    /// Maximum number of requests allowed per window
    /// </summary>
    public int RequestLimit { get; set; } = 100;

    /// <summary>
    /// Time window in seconds
    /// </summary>
    public int WindowInSeconds { get; set; } = 60;

    /// <summary>
    /// Retry after duration in seconds when rate limit is exceeded
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 60;

    /// <summary>
    /// HTTP status code for rate limit exceeded
    /// </summary>
    public int StatusCode { get; set; } = 429;

    /// <summary>
    /// Error message displayed when rate limit is exceeded
    /// </summary>
    public string ErrorMessage { get; set; } = "Rate limit exceeded";

    /// <summary>
    /// Detailed message about the rate limit
    /// </summary>
    public string DetailMessage { get; set; } = "Maximum {0} requests per {1} seconds allowed";
}
