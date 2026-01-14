using Microsoft.Extensions.Logging;
using MoneyBee.Common.Abstractions;
using StackExchange.Redis;

namespace MoneyBee.Common.Infrastructure.Locking;

/// <summary>
/// Redis-based distributed lock implementation for preventing race conditions.
/// Uses Redis SET NX EX for atomic lock acquisition with automatic expiration.
/// </summary>
public class RedisDistributedLockService(
    IConnectionMultiplexer redis,
    ILogger<RedisDistributedLockService> logger) : IDistributedLockService
{

    public async Task<T> ExecuteWithLockAsync<T>(
        string lockKey,
        TimeSpan expiry,
        Func<Task<T>> action)
    {
        if (expiry <= TimeSpan.Zero)
        {
            throw new ArgumentException("Lock expiry must be positive", nameof(expiry));
        }

        var db = redis.GetDatabase();
        var lockValue = Guid.NewGuid().ToString(); // Unique lock value to prevent accidental unlock
        var lockAcquired = false;

        try
        {
            // Try to acquire lock (SET NX EX)
            // Returns true only if key doesn't exist
            lockAcquired = await db.StringSetAsync(
                lockKey,
                lockValue,
                expiry,
                When.NotExists);

            if (!lockAcquired)
            {
                logger.LogWarning(
                    "Failed to acquire distributed lock: {LockKey}. Another process holds the lock.",
                    lockKey);
                throw new TimeoutException($"Could not acquire lock: {lockKey}");
            }

            logger.LogDebug("Acquired distributed lock: {LockKey}", lockKey);

            // Execute the action while holding the lock
            return await action();
        }
        finally
        {
            if (lockAcquired)
            {
                // Release lock only if we acquired it and the value matches
                // This prevents releasing a lock acquired by another process after expiry
                var script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";

                await db.ScriptEvaluateAsync(
                    script,
                    [lockKey],
                    [lockValue]);

                logger.LogDebug("Released distributed lock: {LockKey}", lockKey);
            }
        }
    }
}
