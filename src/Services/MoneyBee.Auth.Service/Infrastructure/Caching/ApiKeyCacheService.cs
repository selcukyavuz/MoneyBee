using System.Diagnostics;
using System.Text.Json;
using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Auth.Service.Infrastructure.Metrics;
using StackExchange.Redis;

namespace MoneyBee.Auth.Service.Infrastructure.Caching;

public class ApiKeyCacheService : IApiKeyCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ApiKeyCacheService> _logger;
    private readonly AuthMetrics? _metrics;
    private const string CacheKeyPrefix = "apikey:validation:";
    private const string AllKeysPattern = "apikey:validation:*";

    public ApiKeyCacheService(
        IConnectionMultiplexer redis,
        ILogger<ApiKeyCacheService> logger,
        AuthMetrics? metrics = null)
    {
        _redis = redis;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<bool?> GetValidationResultAsync(string keyHash)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = GetCacheKey(keyHash);
            var cachedValue = await db.StringGetAsync(cacheKey);

            stopwatch.Stop();
            _metrics?.RecordCacheOperation("get", stopwatch.Elapsed.TotalMilliseconds);

            if (cachedValue.HasValue)
            {
                _metrics?.RecordCacheHit();
                _logger.LogDebug("Cache HIT for key hash: {KeyHash}", MaskKeyHash(keyHash));
                return bool.Parse(cachedValue!);
            }

            _metrics?.RecordCacheMiss();
            _logger.LogDebug("Cache MISS for key hash: {KeyHash}", MaskKeyHash(keyHash));
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from cache for key hash: {KeyHash}", MaskKeyHash(keyHash));
            // Fail open - return null to indicate cache unavailable
            return null;
        }
    }

    public async Task SetValidationResultAsync(string keyHash, bool isValid, TimeSpan expiration)
    {
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = GetCacheKey(keyHash);
            
            await db.StringSetAsync(cacheKey, isValid.ToString(), expiration);
            
            _logger.LogDebug(
                "Cached validation result for key hash: {KeyHash}, IsValid: {IsValid}, Expiration: {Expiration}s",
                MaskKeyHash(keyHash),
                isValid,
                expiration.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to cache for key hash: {KeyHash}", MaskKeyHash(keyHash));
            // Fail open - don't throw, just log
        }
    }

    public async Task InvalidateCacheAsync(string keyHash)
    {
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = GetCacheKey(keyHash);
            
            await db.KeyDeleteAsync(cacheKey);
            
            _logger.LogInformation("Invalidated cache for key hash: {KeyHash}", MaskKeyHash(keyHash));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for key hash: {KeyHash}", MaskKeyHash(keyHash));
        }
    }

    public async Task InvalidateAllCachesAsync()
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints[0]);
            var db = _redis.GetDatabase();

            var keys = server.Keys(pattern: AllKeysPattern);
            var deleteCount = 0;

            foreach (var key in keys)
            {
                await db.KeyDeleteAsync(key);
                deleteCount++;
            }

            _logger.LogInformation("Invalidated {Count} API key validation caches", deleteCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating all caches");
        }
    }

    private static string GetCacheKey(string keyHash)
    {
        return $"{CacheKeyPrefix}{keyHash}";
    }

    private static string MaskKeyHash(string keyHash)
    {
        if (string.IsNullOrEmpty(keyHash) || keyHash.Length < 12)
            return "****";

        return $"{keyHash.Substring(0, 4)}****{keyHash.Substring(keyHash.Length - 4)}";
    }
}
