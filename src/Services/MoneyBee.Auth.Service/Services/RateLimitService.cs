using StackExchange.Redis;

namespace MoneyBee.Auth.Service.Services;

/// <summary>
/// Service interface for rate limiting functionality using Redis sliding window algorithm
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Checks if a request is allowed based on rate limit
    /// </summary>
    /// <param name="identifier">The unique identifier (e.g., API key)</param>
    /// <param name="maxRequests">Maximum number of requests allowed (default: 100)</param>
    /// <param name="window">Time window for rate limiting (default: 1 minute)</param>
    /// <returns>True if request is allowed, false if rate limit exceeded</returns>
    Task<bool> IsRequestAllowedAsync(string identifier, int maxRequests = 100, TimeSpan? window = null);
    
    /// <summary>
    /// Gets the current rate limit information for an identifier
    /// </summary>
    /// <param name="identifier">The unique identifier</param>
    /// <returns>Rate limit information including count, limit, and reset time</returns>
    Task<RateLimitInfo> GetRateLimitInfoAsync(string identifier);
}

/// <summary>
/// Contains rate limit information for an identifier
/// </summary>
public class RateLimitInfo
{
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public DateTime ResetTime { get; set; }
    public int Remaining => Math.Max(0, Limit - RequestCount);
}

public class RateLimitService(
    IConnectionMultiplexer redis,
    ILogger<RateLimitService> logger) : IRateLimitService
{
    
    

    public async Task<bool> IsRequestAllowedAsync(string identifier, int maxRequests = 100, TimeSpan? window = null)
    {
        var windowSpan = window ?? TimeSpan.FromMinutes(1);
        var db = redis.GetDatabase();
        
        var key = $"ratelimit:{identifier}";
        var now = DateTime.UtcNow;
        var windowStart = now.Add(-windowSpan);

        try
        {
            // Remove old entries outside the window
            await db.SortedSetRemoveRangeByScoreAsync(
                key,
                0,
                windowStart.Ticks);

            // Count current requests in window
            var currentCount = await db.SortedSetLengthAsync(key);

            if (currentCount >= maxRequests)
            {
                logger.LogWarning("Rate limit exceeded for {Identifier}", identifier);
                return false;
            }

            // Add current request
            await db.SortedSetAddAsync(key, now.Ticks.ToString(), now.Ticks);

            // Set expiration on the key
            await db.KeyExpireAsync(key, windowSpan.Add(TimeSpan.FromSeconds(10)));

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking rate limit for {Identifier}", identifier);
            // Fail open - allow request if Redis is down
            return true;
        }
    }

    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string identifier)
    {
        var windowSpan = TimeSpan.FromMinutes(1);
        var db = redis.GetDatabase();
        var key = $"ratelimit:{identifier}";
        var now = DateTime.UtcNow;
        var windowStart = now.Add(-windowSpan);

        try
        {
            await db.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart.Ticks);
            var currentCount = (int)await db.SortedSetLengthAsync(key);

            return new RateLimitInfo
            {
                RequestCount = currentCount,
                Limit = 100,
                ResetTime = now.Add(windowSpan)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting rate limit info for {Identifier}", identifier);
            return new RateLimitInfo
            {
                RequestCount = 0,
                Limit = 100,
                ResetTime = now.AddMinutes(1)
            };
        }
    }
}
