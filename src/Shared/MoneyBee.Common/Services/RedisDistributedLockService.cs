using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MoneyBee.Common.Services;

/// <summary>
/// Redis-based distributed lock implementation using SETNX
/// </summary>
public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLockService> _logger;

    public RedisDistributedLockService(
        IConnectionMultiplexer redis,
        ILogger<RedisDistributedLockService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var lockKey = $"lock:{key}";
            var lockValue = Guid.NewGuid().ToString(); // Unique value for this lock instance

            // Use SETNX (SET if Not eXists) with expiration
            var acquired = await db.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);

            if (acquired)
            {
                _logger.LogDebug("Distributed lock acquired: {LockKey}", lockKey);
            }
            else
            {
                _logger.LogDebug("Failed to acquire distributed lock: {LockKey}", lockKey);
            }

            return acquired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring distributed lock: {LockKey}", key);
            throw;
        }
    }

    public async Task ReleaseLockAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var lockKey = $"lock:{key}";
            
            await db.KeyDeleteAsync(lockKey);
            
            _logger.LogDebug("Distributed lock released: {LockKey}", lockKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing distributed lock: {LockKey}", key);
            throw;
        }
    }

    public async Task<T> ExecuteWithLockAsync<T>(
        string key, 
        TimeSpan expiry, 
        Func<Task<T>> action, 
        CancellationToken cancellationToken = default)
    {
        var lockKey = $"lock:{key}";
        
        // Try to acquire lock with retry
        const int maxRetries = 3;
        bool lockAcquired = false;
        
        for (int i = 0; i < maxRetries; i++)
        {
            lockAcquired = await AcquireLockAsync(key, expiry, cancellationToken);
            
            if (lockAcquired)
                break;
                
            if (i < maxRetries - 1)
            {
                _logger.LogDebug("Lock acquisition retry {Attempt}/{MaxRetries} for {LockKey}", 
                    i + 1, maxRetries, lockKey);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (i + 1)), cancellationToken);
            }
        }

        if (!lockAcquired)
        {
            _logger.LogWarning("Failed to acquire lock after {MaxRetries} retries: {LockKey}", 
                maxRetries, lockKey);
            throw new InvalidOperationException(
                $"Could not acquire distributed lock for {key}. Please try again.");
        }

        try
        {
            _logger.LogDebug("Executing action with lock: {LockKey}", lockKey);
            return await action();
        }
        finally
        {
            await ReleaseLockAsync(key, cancellationToken);
        }
    }
}
