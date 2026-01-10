namespace MoneyBee.Auth.Service.Application.Interfaces;

/// <summary>
/// Service for caching API key validation results
/// </summary>
public interface IApiKeyCacheService
{
    /// <summary>
    /// Get cached validation result for an API key hash
    /// </summary>
    Task<bool?> GetValidationResultAsync(string keyHash);

    /// <summary>
    /// Cache a validation result for an API key hash
    /// </summary>
    Task SetValidationResultAsync(string keyHash, bool isValid, TimeSpan expiration);

    /// <summary>
    /// Invalidate cache for a specific API key hash
    /// </summary>
    Task InvalidateCacheAsync(string keyHash);

    /// <summary>
    /// Invalidate all API key validation caches
    /// </summary>
    Task InvalidateAllCachesAsync();
}
