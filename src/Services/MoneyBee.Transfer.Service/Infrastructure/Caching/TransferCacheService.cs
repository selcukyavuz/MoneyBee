using System.Diagnostics;
using System.Text.Json;
using MoneyBee.Transfer.Service.Application.DTOs;
using MoneyBee.Transfer.Service.Infrastructure.Metrics;
using StackExchange.Redis;

namespace MoneyBee.Transfer.Service.Infrastructure.Caching;

public interface ITransferCacheService
{
    Task<TransferDto?> GetTransferAsync(Guid transferId);
    Task SetTransferAsync(Guid transferId, TransferDto transfer, TimeSpan? expiration = null);
    Task<IEnumerable<TransferDto>?> GetTransfersByCustomerAsync(Guid customerId);
    Task SetTransfersByCustomerAsync(Guid customerId, IEnumerable<TransferDto> transfers, TimeSpan? expiration = null);
    Task InvalidateTransferAsync(Guid transferId);
    Task InvalidateCustomerTransfersAsync(Guid customerId);
}

public class TransferCacheService : ITransferCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<TransferCacheService> _logger;
    private readonly TransferMetrics? _metrics;
    private const string TransferCacheKeyPrefix = "transfer:";
    private const string CustomerTransfersCacheKeyPrefix = "transfer:customer:";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public TransferCacheService(
        IConnectionMultiplexer redis,
        ILogger<TransferCacheService> logger,
        TransferMetrics? metrics = null)
    {
        _redis = redis;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<TransferDto?> GetTransferAsync(Guid transferId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{TransferCacheKeyPrefix}{transferId}";
            var cachedValue = await db.StringGetAsync(cacheKey);

            stopwatch.Stop();
            _metrics?.RecordCacheOperation("get", stopwatch.Elapsed.TotalMilliseconds);

            if (cachedValue.HasValue)
            {
                _metrics?.RecordCacheHit();
                _logger.LogDebug("Cache HIT for transfer: {TransferId}", transferId);
                return JsonSerializer.Deserialize<TransferDto>(cachedValue!);
            }

            _metrics?.RecordCacheMiss();
            _logger.LogDebug("Cache MISS for transfer: {TransferId}", transferId);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting transfer from cache: {TransferId}", transferId);
            return null;
        }
    }

    public async Task SetTransferAsync(Guid transferId, TransferDto transfer, TimeSpan? expiration = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{TransferCacheKeyPrefix}{transferId}";
            var serialized = JsonSerializer.Serialize(transfer);
            
            await db.StringSetAsync(cacheKey, serialized, expiration ?? DefaultExpiration);
            
            stopwatch.Stop();
            _metrics?.RecordCacheOperation("set", stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogDebug("Transfer cached: {TransferId}", transferId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error setting transfer in cache: {TransferId}", transferId);
        }
    }

    public async Task<IEnumerable<TransferDto>?> GetTransfersByCustomerAsync(Guid customerId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{CustomerTransfersCacheKeyPrefix}{customerId}";
            var cachedValue = await db.StringGetAsync(cacheKey);

            stopwatch.Stop();
            _metrics?.RecordCacheOperation("get", stopwatch.Elapsed.TotalMilliseconds);

            if (cachedValue.HasValue)
            {
                _metrics?.RecordCacheHit();
                _logger.LogDebug("Cache HIT for customer transfers: {CustomerId}", customerId);
                return JsonSerializer.Deserialize<IEnumerable<TransferDto>>(cachedValue!);
            }

            _metrics?.RecordCacheMiss();
            _logger.LogDebug("Cache MISS for customer transfers: {CustomerId}", customerId);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting customer transfers from cache: {CustomerId}", customerId);
            return null;
        }
    }

    public async Task SetTransfersByCustomerAsync(Guid customerId, IEnumerable<TransferDto> transfers, TimeSpan? expiration = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{CustomerTransfersCacheKeyPrefix}{customerId}";
            var serialized = JsonSerializer.Serialize(transfers);
            
            await db.StringSetAsync(cacheKey, serialized, expiration ?? DefaultExpiration);
            
            stopwatch.Stop();
            _metrics?.RecordCacheOperation("set", stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogDebug("Customer transfers cached: {CustomerId}", customerId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error setting customer transfers in cache: {CustomerId}", customerId);
        }
    }

    public async Task InvalidateTransferAsync(Guid transferId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{TransferCacheKeyPrefix}{transferId}";
            await db.KeyDeleteAsync(cacheKey);
            
            stopwatch.Stop();
            _metrics?.RecordCacheOperation("delete", stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogDebug("Transfer cache invalidated: {TransferId}", transferId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invalidating transfer cache: {TransferId}", transferId);
        }
    }

    public async Task InvalidateCustomerTransfersAsync(Guid customerId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"{CustomerTransfersCacheKeyPrefix}{customerId}";
            await db.KeyDeleteAsync(cacheKey);
            
            stopwatch.Stop();
            _metrics?.RecordCacheOperation("delete", stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogDebug("Customer transfers cache invalidated: {CustomerId}", customerId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invalidating customer transfers cache: {CustomerId}", customerId);
        }
    }
}
