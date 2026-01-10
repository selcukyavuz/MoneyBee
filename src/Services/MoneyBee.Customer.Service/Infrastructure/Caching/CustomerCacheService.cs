using System.Diagnostics;
using System.Text.Json;
using MoneyBee.Customer.Service.Application.DTOs;
using MoneyBee.Customer.Service.Infrastructure.Metrics;
using StackExchange.Redis;

namespace MoneyBee.Customer.Service.Infrastructure.Caching;

public interface ICustomerCacheService
{
    Task<CustomerDto?> GetCustomerAsync(Guid customerId);
    Task SetCustomerAsync(Guid customerId, CustomerDto customer, TimeSpan? expiration = null);
    Task<CustomerDto?> GetCustomerByNationalIdAsync(string nationalId);
    Task SetCustomerByNationalIdAsync(string nationalId, CustomerDto customer, TimeSpan? expiration = null);
    Task InvalidateCustomerAsync(Guid customerId);
    Task InvalidateCustomerByNationalIdAsync(string nationalId);
}

public class CustomerCacheService : ICustomerCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CustomerCacheService> _logger;
    private readonly CustomerMetrics? _metrics;
    private const string CustomerCacheKeyPrefix = "customer:";
    private const string NationalIdCacheKeyPrefix = "customer:nationalid:";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public CustomerCacheService(
        IConnectionMultiplexer redis,
        ILogger<CustomerCacheService> logger,
        CustomerMetrics? metrics = null)
    {
        _redis = redis;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid customerId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{CustomerCacheKeyPrefix}{customerId}";
            var cachedValue = await db.StringGetAsync(cacheKey);

            stopwatch.Stop();
            _metrics?.RecordCacheOperation("get", stopwatch.Elapsed.TotalMilliseconds);

            if (cachedValue.HasValue)
            {
                _metrics?.RecordCacheHit();
                _logger.LogDebug("Cache HIT for customer: {CustomerId}", customerId);
                return JsonSerializer.Deserialize<CustomerDto>(cachedValue!);
            }

            _metrics?.RecordCacheMiss();
            _logger.LogDebug("Cache MISS for customer: {CustomerId}", customerId);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting customer from cache: {CustomerId}", customerId);
            return null;
        }
    }

    public async Task SetCustomerAsync(Guid customerId, CustomerDto customer, TimeSpan? expiration = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{CustomerCacheKeyPrefix}{customerId}";
            var serialized = JsonSerializer.Serialize(customer);
            
            await db.StringSetAsync(cacheKey, serialized, expiration ?? DefaultExpiration);
            
            stopwatch.Stop();
            _metrics?.RecordCacheOperation("set", stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogDebug("Customer cached: {CustomerId}", customerId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error setting customer in cache: {CustomerId}", customerId);
        }
    }

    public async Task<CustomerDto?> GetCustomerByNationalIdAsync(string nationalId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{NationalIdCacheKeyPrefix}{nationalId}";
            var cachedValue = await db.StringGetAsync(cacheKey);

            stopwatch.Stop();
            _metrics?.RecordCacheOperation("get", stopwatch.Elapsed.TotalMilliseconds);

            if (cachedValue.HasValue)
            {
                _metrics?.RecordCacheHit();
                _logger.LogDebug("Cache HIT for national ID: {NationalId}", MaskNationalId(nationalId));
                return JsonSerializer.Deserialize<CustomerDto>(cachedValue!);
            }

            _metrics?.RecordCacheMiss();
            _logger.LogDebug("Cache MISS for national ID: {NationalId}", MaskNationalId(nationalId));
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting customer by national ID from cache");
            return null;
        }
    }

    public async Task SetCustomerByNationalIdAsync(string nationalId, CustomerDto customer, TimeSpan? expiration = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{NationalIdCacheKeyPrefix}{nationalId}";
            var serialized = JsonSerializer.Serialize(customer);
            
            await db.StringSetAsync(cacheKey, serialized, expiration ?? DefaultExpiration);
            
            stopwatch.Stop();
            _metrics?.RecordCacheOperation("set", stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogDebug("Customer cached by national ID: {NationalId}", MaskNationalId(nationalId));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error setting customer by national ID in cache");
        }
    }

    public async Task InvalidateCustomerAsync(Guid customerId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{CustomerCacheKeyPrefix}{customerId}";
            await db.KeyDeleteAsync(cacheKey);
            
            stopwatch.Stop();
            _metrics?.RecordCacheOperation("delete", stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogDebug("Customer cache invalidated: {CustomerId}", customerId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invalidating customer cache: {CustomerId}", customerId);
        }
    }

    public async Task InvalidateCustomerByNationalIdAsync(string nationalId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{NationalIdCacheKeyPrefix}{nationalId}";
            await db.KeyDeleteAsync(cacheKey);
            
            stopwatch.Stop();
            _metrics?.RecordCacheOperation("delete", stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogDebug("Customer cache invalidated by national ID: {NationalId}", MaskNationalId(nationalId));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invalidating customer cache by national ID");
        }
    }

    private static string MaskNationalId(string nationalId)
    {
        if (string.IsNullOrEmpty(nationalId) || nationalId.Length < 4)
            return "***";
        
        return $"{nationalId.Substring(0, 2)}...{nationalId.Substring(nationalId.Length - 2)}";
    }
}
