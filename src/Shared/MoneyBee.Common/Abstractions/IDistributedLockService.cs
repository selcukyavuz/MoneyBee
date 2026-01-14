namespace MoneyBee.Common.Abstractions;

/// <summary>
/// Distributed lock service for coordinating concurrent operations across multiple service instances.
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Executes an action while holding a distributed lock.
    /// If the lock cannot be acquired within the expiry time, throws TimeoutException.
    /// </summary>
    /// <typeparam name="T">Return type of the action</typeparam>
    /// <param name="lockKey">Unique key identifying the resource to lock</param>
    /// <param name="expiry">Maximum time to hold the lock</param>
    /// <param name="action">Action to execute while holding the lock</param>
    /// <returns>Result of the action</returns>
    Task<T> ExecuteWithLockAsync<T>(
        string lockKey,
        TimeSpan expiry,
        Func<Task<T>> action);
}
