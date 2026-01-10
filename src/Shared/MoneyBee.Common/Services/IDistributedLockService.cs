namespace MoneyBee.Common.Services;

/// <summary>
/// Distributed lock service for preventing race conditions across multiple service instances
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Acquires a distributed lock
    /// </summary>
    /// <param name="key">Unique lock key</param>
    /// <param name="expiry">Lock expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if lock acquired, false otherwise</returns>
    Task<bool> AcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Releases a distributed lock
    /// </summary>
    /// <param name="key">Lock key to release</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReleaseLockAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes an action with a distributed lock
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="key">Lock key</param>
    /// <param name="expiry">Lock expiration time</param>
    /// <param name="action">Action to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the action</returns>
    Task<T> ExecuteWithLockAsync<T>(
        string key, 
        TimeSpan expiry, 
        Func<Task<T>> action, 
        CancellationToken cancellationToken = default);
}
