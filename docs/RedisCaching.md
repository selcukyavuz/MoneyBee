# Redis Caching Implementation

## Overview
Auth Service now implements distributed caching using Redis to significantly improve API key validation performance.

## Architecture

### Cache-Aside Pattern
The implementation uses the **cache-aside (lazy loading)** pattern:

```
1. Request → Check Cache
2. Cache Hit? → Return cached result
3. Cache Miss? → Query Database → Cache result → Return
4. On Update/Delete → Invalidate cache
```

### Cache Service
**Location:** `Infrastructure/Caching/ApiKeyCacheService.cs`

**Interface:** `IApiKeyCacheService`

**Key Features:**
- ✅ **Fail-Open Design**: If Redis is unavailable, requests proceed without caching (degrades gracefully)
- ✅ **Key Masking**: Logs never expose full key hashes (security)
- ✅ **Configurable Expiration**: Different TTLs for positive/negative results
- ✅ **Automatic Invalidation**: Cache cleared on updates/deletes

## Implementation Details

### 1. Validation Caching

**Before (No Cache):**
```csharp
public async Task<bool> ValidateApiKeyAsync(string apiKey)
{
    var keyHash = ApiKeyHelper.HashApiKey(apiKey);
    var key = await _repository.GetByKeyHashAsync(keyHash); // DB query every time
    return key != null && key.IsActive;
}
```

**After (With Cache):**
```csharp
public async Task<bool> ValidateApiKeyAsync(string apiKey)
{
    var keyHash = ApiKeyHelper.HashApiKey(apiKey);
    
    // Check cache first
    var cachedResult = await _cacheService.GetValidationResultAsync(keyHash);
    if (cachedResult.HasValue)
        return cachedResult.Value; // Cache hit!
    
    // Cache miss - query database
    var key = await _repository.GetByKeyHashAsync(keyHash);
    var isValid = key != null && key.IsActive;
    
    // Cache result
    var ttl = isValid ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(1);
    await _cacheService.SetValidationResultAsync(keyHash, isValid, ttl);
    
    return isValid;
}
```

### 2. Cache Invalidation

**On Update:**
```csharp
public async Task<ApiKeyDto?> UpdateApiKeyAsync(Guid id, UpdateApiKeyRequest request)
{
    var key = await _repository.GetByIdAsync(id);
    // ... update logic
    var updated = await _repository.UpdateAsync(key);
    
    // Invalidate cache since status/details changed
    await _cacheService.InvalidateCacheAsync(updated.KeyHash);
    
    return MapToDto(updated);
}
```

**On Delete:**
```csharp
public async Task<bool> DeleteApiKeyAsync(Guid id)
{
    var key = await _repository.GetByIdAsync(id);
    var deleted = await _repository.DeleteAsync(id);
    
    if (deleted)
    {
        // Remove from cache
        await _cacheService.InvalidateCacheAsync(key.KeyHash);
    }
    
    return deleted;
}
```

### 3. Cache Key Strategy

```
Pattern: apikey:validation:{keyHash}

Examples:
- apikey:validation:a1b2c3d4e5f6...
- apikey:validation:xyz789abc123...
```

**Benefits:**
- ✅ Namespace isolation (won't conflict with rate limiting keys)
- ✅ Easy pattern matching for bulk invalidation
- ✅ Human-readable in Redis CLI

## Configuration

**appsettings.json:**
```json
{
  "Caching": {
    "ApiKeyValidation": {
      "ExpirationMinutes": 5,
      "NegativeResultExpirationMinutes": 1
    }
  }
}
```

**TTL Strategy:**
- **Valid Keys**: 5 minutes (longer cache for frequently used keys)
- **Invalid Keys**: 1 minute (shorter to allow quick reactivation)

## Performance Impact

### Before Caching:
```
Every validation = 1 DB query
1000 req/s = 1000 DB queries/s
```

### After Caching:
```
First validation = 1 DB query + 1 Redis write
Subsequent validations (within 5 min) = 1 Redis read
1000 req/s with 95% cache hit = 50 DB queries/s + 950 Redis reads/s
```

**Expected Improvements:**
- 🚀 **95%+ reduction in database load**
- 🚀 **~10x faster response time** (Redis in-memory vs PostgreSQL disk I/O)
- 🚀 **Better scalability** (Redis can handle 100k+ ops/s)

## Testing

**Test Coverage:** 10 comprehensive tests

**Location:** `tests/MoneyBee.Auth.Service.UnitTests/Infrastructure/Caching/ApiKeyCacheServiceTests.cs`

**Test Scenarios:**
```csharp
✅ GetValidationResultAsync_WithCachedValue_ShouldReturnCachedResult
✅ GetValidationResultAsync_WithNoCachedValue_ShouldReturnNull
✅ GetValidationResultAsync_WithRedisException_ShouldReturnNullAndLog
✅ SetValidationResultAsync_WithValidData_ShouldCacheResult
✅ SetValidationResultAsync_WithRedisException_ShouldNotThrow
✅ InvalidateCacheAsync_ShouldDeleteKey
✅ InvalidateCacheAsync_WithRedisException_ShouldNotThrow
✅ GetValidationResultAsync_WithDifferentBoolValues_ShouldParseCorrectly
✅ SetValidationResultAsync_WithFalseValue_ShouldCacheFalse
✅ (Plus resilience tests for Redis failures)
```

**Run Tests:**
```bash
dotnet test tests/MoneyBee.Auth.Service.UnitTests
```

## Monitoring

### Cache Metrics to Monitor:

**1. Hit Rate:**
```
Hit Rate = Cache Hits / Total Requests
Target: >90%
```

**2. Database Load:**
```
Before: 1000 queries/s
After: <100 queries/s (with good hit rate)
```

**3. Redis Operations:**
```bash
# Redis CLI
redis-cli
> INFO stats
> KEYS apikey:validation:*
> GET apikey:validation:{someHash}
```

### Logs to Watch:

```log
[INFO] Cache HIT for key hash: a1b2****c3d4
[INFO] Cache MISS for key hash: xyz7****9abc
[INFO] Cached validation result for key hash: ..., IsValid: True, Expiration: 300s
[INFO] Invalidated cache for key hash: ...
[ERROR] Error reading from cache for key hash: ... (Redis connection failed)
```

## Failure Modes

### Redis Unavailable:
```
✅ Service continues to work (fails open)
✅ All requests hit database (degraded performance)
✅ Errors logged for monitoring
✅ No requests fail due to cache issues
```

### Redis Slow:
```
✅ Timeout configured on connection
✅ Falls back to database if cache times out
✅ Alerts should be configured for high latency
```

## Future Enhancements

### Potential Improvements:

1. **Cache Warming**: Pre-populate cache for known active keys on startup
2. **Metrics Export**: Prometheus metrics for cache hit rate, latency
3. **Multiple Redis Nodes**: Redis Sentinel for high availability
4. **Cache Compression**: For keys with large metadata
5. **TTL Optimization**: Dynamic TTL based on key usage patterns

### Advanced Patterns:

```csharp
// Write-Through Pattern (for critical data):
await _repository.UpdateAsync(key);
await _cacheService.SetValidationResultAsync(key.KeyHash, true, ttl);

// Read-Through Pattern (automatic cache population):
public async Task<bool> ValidateWithReadThrough(string keyHash)
{
    return await _cacheService.GetOrCreateAsync(
        keyHash,
        async () => await _repository.GetByKeyHashAsync(keyHash)
    );
}
```

## Troubleshooting

### Issue: Low Cache Hit Rate
**Causes:**
- TTL too short
- High update/delete frequency
- Keys not used frequently enough

**Solutions:**
- Increase TTL for valid keys
- Analyze access patterns
- Consider separate caching for read-heavy keys

### Issue: Memory Usage High
**Causes:**
- Too many cached keys
- TTL too long
- Memory not being reclaimed

**Solutions:**
```bash
# Check Redis memory
redis-cli INFO memory

# Set max memory policy
redis-cli CONFIG SET maxmemory-policy allkeys-lru
redis-cli CONFIG SET maxmemory 256mb
```

### Issue: Stale Data
**Causes:**
- Cache not invalidated on updates
- Long TTL

**Solutions:**
- Verify invalidation logic in update/delete methods
- Reduce TTL if consistency is critical
- Use pub/sub for distributed invalidation

## Best Practices

1. ✅ **Always fail open** - Don't let cache failures break the service
2. ✅ **Log cache operations** - Essential for debugging and monitoring
3. ✅ **Mask sensitive data** - Never log full keys or hashes
4. ✅ **Use appropriate TTLs** - Balance freshness vs. performance
5. ✅ **Invalidate proactively** - Don't wait for TTL expiration on updates
6. ✅ **Monitor hit rate** - <90% may indicate issues
7. ✅ **Test failure scenarios** - Redis down, slow, etc.

---

**Status:** ✅ Production Ready

**Last Updated:** 2026-01-10

**Next Review:** After observing production metrics for 1 week
