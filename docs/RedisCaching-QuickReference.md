# Redis Caching - Quick Reference

## 📊 Key Metrics

| Metric | Value | Notes |
|--------|-------|-------|
| Cache TTL (Valid) | 5 minutes | Positive validation results |
| Cache TTL (Invalid) | 1 minute | Negative results (faster reactivation) |
| Expected Hit Rate | >90% | Target for production |
| DB Load Reduction | 95%+ | With good hit rate |
| Response Time | ~10x faster | vs direct DB queries |

## 🔧 Quick Commands

### Redis CLI Inspection:
```bash
# Connect to Redis
redis-cli

# List all API key caches
KEYS apikey:validation:*

# Get specific cache entry
GET apikey:validation:{keyHash}

# Check cache stats
INFO stats

# Monitor cache operations in real-time
MONITOR

# Clear all API key caches
KEYS apikey:validation:* | xargs redis-cli DEL

# Check memory usage
INFO memory
```

### Application Logs:
```bash
# Watch cache hits/misses
tail -f logs/auth-service*.log | grep "Cache"

# Count cache hits
grep "Cache HIT" logs/auth-service*.log | wc -l

# Count cache misses
grep "Cache MISS" logs/auth-service*.log | wc -l

# Calculate hit rate
echo "scale=2; $(grep "Cache HIT" logs/*.log | wc -l) / $(grep "Cache" logs/*.log | wc -l) * 100" | bc
```

## 🎯 Cache Behavior

### Validation Flow:
```
1. Request with API Key
   ↓
2. Hash the key (SHA256)
   ↓
3. Check Redis: apikey:validation:{hash}
   ↓
   ├─ HIT → Return cached result (fast!)
   └─ MISS → Query PostgreSQL
              ↓
              Cache result in Redis
              ↓
              Return result
```

### Cache Invalidation:
```
Update API Key → Invalidate cache → Next request = fresh DB query
Delete API Key → Invalidate cache → Next request = fresh DB query
```

## 📝 Configuration

**Location:** `appsettings.json`
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Caching": {
    "ApiKeyValidation": {
      "ExpirationMinutes": 5,
      "NegativeResultExpirationMinutes": 1
    }
  }
}
```

## 🧪 Testing

### Run Cache Tests:
```bash
# All Auth Service tests (includes 10 cache tests)
dotnet test tests/MoneyBee.Auth.Service.UnitTests

# Only cache tests
dotnet test tests/MoneyBee.Auth.Service.UnitTests --filter "FullyQualifiedName~ApiKeyCacheServiceTests"

# All tests
dotnet test MoneyBee.sln --filter "FullyQualifiedName~UnitTests"
```

### Test Results:
```
✅ 29 tests passing (Auth Service)
   - 14 ApiKeyHelper tests
   - 10 ApiKeyCacheService tests
   - 5 other tests

✅ 63 tests total (all services)
```

## 🔍 Troubleshooting

### Low Hit Rate (<80%):

**Check:**
1. Are keys being updated frequently?
2. Is TTL too short?
3. Are there many unique API keys with low usage?

**Fix:**
```bash
# Check update frequency
grep "API Key updated" logs/*.log | wc -l

# Monitor cache patterns
redis-cli MONITOR | grep "apikey:validation"

# Increase TTL if needed (appsettings.json)
"ExpirationMinutes": 10  # up from 5
```

### Redis Connection Issues:

**Symptoms:**
- Logs show "Error reading from cache"
- Service still works (fail-open design)

**Fix:**
```bash
# Check Redis status
docker ps | grep redis
docker logs moneybee-redis

# Restart Redis
docker restart moneybee-redis

# Test connection
redis-cli PING  # Should return PONG
```

### High Memory Usage:

**Check:**
```bash
# Redis memory info
redis-cli INFO memory

# Count cached keys
redis-cli KEYS "apikey:validation:*" | wc -l

# Check key sizes
redis-cli --bigkeys
```

**Fix:**
```bash
# Set memory limit
redis-cli CONFIG SET maxmemory 256mb

# Set eviction policy (LRU = Least Recently Used)
redis-cli CONFIG SET maxmemory-policy allkeys-lru

# Or clear old caches manually
redis-cli FLUSHDB
```

## 📈 Performance Testing

### Benchmark Cache Performance:
```bash
# Install redis-benchmark (comes with Redis)
redis-benchmark -t get,set -n 100000 -q

# Test specific operations
redis-benchmark -t set -n 10000 -d 100 -q
redis-benchmark -t get -n 10000 -d 100 -q
```

### Expected Results:
```
SET: ~50,000 requests/sec
GET: ~80,000 requests/sec
```

### API Load Test:
```bash
# Install Apache Bench
brew install apache-bench  # macOS

# Test validation endpoint (with caching)
ab -n 1000 -c 10 -H "X-API-Key: your-key" \
   http://localhost:5001/api/validation

# Compare before/after caching:
# Before: ~100 req/s (DB bottleneck)
# After: ~1000 req/s (Redis fast!)
```

## 🎓 Best Practices

1. ✅ **Monitor hit rate daily** - Alert if <80%
2. ✅ **Set Redis memory limit** - Prevent OOM
3. ✅ **Use connection pooling** - Already done via IConnectionMultiplexer
4. ✅ **Log cache operations** - Essential for debugging
5. ✅ **Test Redis failures** - Ensure fail-open works
6. ✅ **Version cache keys** - If changing data structure: `apikey:validation:v2:{hash}`

## 🚀 Production Checklist

- [ ] Redis memory limit configured (256MB+)
- [ ] Eviction policy set (allkeys-lru)
- [ ] Redis persistence enabled (AOF or RDB)
- [ ] Monitoring configured (cache hit rate, memory usage)
- [ ] Alerts for low hit rate (<80%)
- [ ] Alerts for Redis connection failures
- [ ] Load testing completed
- [ ] Backup Redis config

## 📚 Related Documentation

- [Full Caching Guide](RedisCaching.md)
- [Test Documentation](../tests/README.md)
- [Main README](../README.md)

---

**Quick Help:**
- Cache not working? Check Redis: `redis-cli PING`
- Low hit rate? Check logs: `grep "Cache" logs/*.log`
- Need to clear cache? `redis-cli FLUSHDB`
- Want metrics? `redis-cli INFO stats`
