using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MoneyBee.Common.Services;

/// <summary>
/// API key validator with Redis caching and HTTP fallback to Auth Service
/// </summary>
public class CachedApiKeyValidator : IApiKeyValidator
{
    private readonly IDistributedCache _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CachedApiKeyValidator> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "apikey:";

    public CachedApiKeyValidator(
        IDistributedCache cache,
        HttpClient httpClient,
        ILogger<CachedApiKeyValidator> logger)
    {
        _cache = cache;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Validates an API key using Redis cache with HTTP fallback to Auth Service
    /// </summary>
    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        var cacheKey = $"{CacheKeyPrefix}{apiKey}";

        try
        {
            // Try to get from cache first
            var cachedValue = await _cache.GetStringAsync(cacheKey);
            if (cachedValue != null)
            {
                _logger.LogDebug("API key validation result found in cache");
                return cachedValue == "valid";
            }

            // Cache miss - call Auth Service
            _logger.LogDebug("API key not in cache, calling Auth Service");
            var isValid = await ValidateViaAuthServiceAsync(apiKey);

            // Cache the result
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            };
            await _cache.SetStringAsync(cacheKey, isValid ? "valid" : "invalid", cacheOptions);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            // On cache or HTTP failure, deny access for security
            return false;
        }
    }

    private async Task<bool> ValidateViaAuthServiceAsync(string apiKey)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/apikeys/validate", new { ApiKey = apiKey });
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auth Service returned {StatusCode} for API key validation", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<ValidationResponse>();
            return result?.IsValid ?? false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Auth Service for validation");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Auth Service");
            return false;
        }
    }

    private class ValidationResponse
    {
        public bool IsValid { get; set; }
    }
}
