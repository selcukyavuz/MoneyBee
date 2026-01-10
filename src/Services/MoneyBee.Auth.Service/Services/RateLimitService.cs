using StackExchange.Redis;

namespace MoneyBee.Auth.Service.Services;

public interface IRateLimitService
{
    Task<bool> IsRequestAllowedAsync(string identifier, int maxRequests = 100, TimeSpan? window = null);
    Task<RateLimitInfo> GetRateLimitInfoAsync(string identifier);
}

public class RateLimitInfo
{
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public DateTime ResetTime { get; set; }
    public int Remaining => Math.Max(0, Limit - RequestCount);
}

public class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(
        IConnectionMultiplexer redis,
        ILogger<RateLimitService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> IsRequestAllowedAsync(string identifier, int maxRequests = 100, TimeSpan? window = null)
    {
        var windowSpan = window ?? TimeSpan.FromMinutes(1);
        var db = _redis.GetDatabase();
        
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
                _logger.LogWarning("Rate limit exceeded for {Identifier}", identifier);
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
            _logger.LogError(ex, "Error checking rate limit for {Identifier}", identifier);
            // Fail open - allow request if Redis is down
            return true;
        }
    }

    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string identifier)
    {
        var windowSpan = TimeSpan.FromMinutes(1);
        var db = _redis.GetDatabase();
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
            _logger.LogError(ex, "Error getting rate limit info for {Identifier}", identifier);
            return new RateLimitInfo
            {
                RequestCount = 0,
                Limit = 100,
                ResetTime = now.AddMinutes(1)
            };
        }
    }
}
