# OpenTelemetry Metrics - MoneyBee Auth Service

## Overview

MoneyBee Auth Service uses OpenTelemetry for comprehensive observability with Prometheus-compatible metrics export. This enables real-time monitoring, alerting, and performance analysis.

## Architecture

### Components

- **OpenTelemetry SDK**: Industry-standard observability framework
- **Prometheus Exporter**: Exposes metrics in Prometheus format
- **Custom Metrics**: Business-specific metrics for Auth Service
- **ASP.NET Core Instrumentation**: Automatic HTTP request/response metrics
- **HTTP Client Instrumentation**: Outbound HTTP call metrics

### Metrics Endpoint

- **URL**: `http://localhost:5001/metrics`
- **Format**: Prometheus text format
- **Scrape Interval**: 15 seconds (recommended)

## Available Metrics

### Custom Auth Metrics (MoneyBee.Auth.Service)

#### Counters

| Metric Name | Type | Description | Tags |
|------------|------|-------------|------|
| `auth_apikey_validations_total` | Counter | Total API key validation attempts | `result` (valid/invalid) |
| `auth_cache_hits_total` | Counter | Total cache hits | - |
| `auth_cache_misses_total` | Counter | Total cache misses | - |
| `auth_apikey_created_total` | Counter | Total API keys created | - |
| `auth_apikey_deleted_total` | Counter | Total API keys deleted | - |
| `auth_apikey_updated_total` | Counter | Total API keys updated | - |

#### Histograms

| Metric Name | Type | Description | Tags | Unit |
|------------|------|-------------|------|------|
| `auth_apikey_validation_duration` | Histogram | API key validation duration | - | milliseconds |
| `auth_cache_operation_duration` | Histogram | Cache operation duration | `operation` (get/set/delete) | milliseconds |

#### Gauges

| Metric Name | Type | Description | Tags |
|------------|------|-------------|------|
| `auth_apikey_active_count` | ObservableGauge | Current number of active API keys | - |

### ASP.NET Core Metrics (Automatic)

- `http_server_request_duration` - HTTP request duration
- `http_server_active_requests` - Currently active requests
- `kestrel_connection_duration` - Connection duration
- `kestrel_active_connections` - Active connections

### HTTP Client Metrics (Automatic)

- `http_client_request_duration` - Outbound HTTP call duration
- `http_client_active_requests` - Active outbound requests

## Implementation Details

### AuthMetrics Service

Located at: `Infrastructure/Metrics/AuthMetrics.cs`

```csharp
public class AuthMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _validationCounter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;
    // ... more metrics

    public AuthMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("MoneyBee.Auth.Service");
        
        _validationCounter = _meter.CreateCounter<long>(
            "auth.apikey.validations.total",
            description: "Total number of API key validations");
        
        // ... initialize other metrics
    }

    public void RecordValidation(bool isValid, double durationMs)
    {
        _validationCounter.Add(1, new KeyValuePair<string, object?>("result", isValid ? "valid" : "invalid"));
        _validationDurationHistogram.Record(durationMs);
    }
}
```

### Integration Points

#### ApiKeyCacheService

Tracks:
- Cache hits/misses
- Cache operation duration (get, set, delete)

```csharp
public async Task<bool?> GetValidationResultAsync(string keyHash)
{
    var stopwatch = Stopwatch.StartNew();
    // ... cache logic ...
    
    if (cachedValue.HasValue)
        _metrics?.RecordCacheHit();
    else
        _metrics?.RecordCacheMiss();
    
    _metrics?.RecordCacheOperation("get", stopwatch.Elapsed.TotalMilliseconds);
    return cachedValue;
}
```

#### ApiKeyService

Tracks:
- Validation attempts (success/failure)
- Validation duration
- CRUD operations (create, update, delete)

```csharp
public async Task<bool> ValidateApiKeyAsync(string apiKey)
{
    var stopwatch = Stopwatch.StartNew();
    // ... validation logic ...
    
    stopwatch.Stop();
    _metrics?.RecordValidation(isValid, stopwatch.Elapsed.TotalMilliseconds);
    return isValid;
}
```

## Configuration

### Program.cs Setup

```csharp
// Register metrics service
builder.Services.AddSingleton<AuthMetrics>();

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "MoneyBee.Auth.Service",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName))
    .WithMetrics(metrics => metrics
        .AddMeter("MoneyBee.Auth.Service")           // Custom metrics
        .AddAspNetCoreInstrumentation()              // HTTP server metrics
        .AddHttpClientInstrumentation()              // HTTP client metrics
        .AddPrometheusExporter());                   // Prometheus format

// Map metrics endpoint
app.MapPrometheusScrapingEndpoint();
```

## Prometheus Configuration

### prometheus.yml

```yaml
scrape_configs:
  - job_name: 'moneybee-auth-service'
    scrape_interval: 15s
    static_configs:
      - targets: ['localhost:5001']
        labels:
          service: 'auth-service'
          environment: 'development'
```

## Grafana Dashboard

### Key Panels

#### 1. API Key Validation Rate
```promql
# Success rate
rate(auth_apikey_validations_total{result="valid"}[5m]) 
/ 
rate(auth_apikey_validations_total[5m]) * 100

# Total validations per second
rate(auth_apikey_validations_total[5m])
```

#### 2. Cache Performance
```promql
# Cache hit rate
rate(auth_cache_hits_total[5m]) 
/ 
(rate(auth_cache_hits_total[5m]) + rate(auth_cache_misses_total[5m])) * 100

# Cache operations per second
rate(auth_cache_hits_total[5m]) + rate(auth_cache_misses_total[5m])
```

#### 3. Validation Latency
```promql
# P50, P95, P99 latencies
histogram_quantile(0.50, rate(auth_apikey_validation_duration_bucket[5m]))
histogram_quantile(0.95, rate(auth_apikey_validation_duration_bucket[5m]))
histogram_quantile(0.99, rate(auth_apikey_validation_duration_bucket[5m]))
```

#### 4. Active API Keys
```promql
# Current active API keys
auth_apikey_active_count
```

#### 5. HTTP Request Rate
```promql
# Requests per second by endpoint
rate(http_server_request_duration_count[5m])

# Average response time
rate(http_server_request_duration_sum[5m]) 
/ 
rate(http_server_request_duration_count[5m])
```

## Alerting Rules

### Prometheus Alerts

```yaml
groups:
  - name: auth_service_alerts
    interval: 30s
    rules:
      # Low cache hit rate
      - alert: LowCacheHitRate
        expr: |
          rate(auth_cache_hits_total[5m]) 
          / 
          (rate(auth_cache_hits_total[5m]) + rate(auth_cache_misses_total[5m])) < 0.80
        for: 5m
        labels:
          severity: warning
          service: auth-service
        annotations:
          summary: "Cache hit rate below 80%"
          description: "Cache hit rate is {{ $value | humanizePercentage }}"

      # High validation failure rate
      - alert: HighValidationFailureRate
        expr: |
          rate(auth_apikey_validations_total{result="invalid"}[5m])
          /
          rate(auth_apikey_validations_total[5m]) > 0.30
        for: 5m
        labels:
          severity: warning
          service: auth-service
        annotations:
          summary: "High API key validation failure rate"
          description: "Validation failure rate is {{ $value | humanizePercentage }}"

      # High validation latency
      - alert: HighValidationLatency
        expr: |
          histogram_quantile(0.95, rate(auth_apikey_validation_duration_bucket[5m])) > 100
        for: 5m
        labels:
          severity: warning
          service: auth-service
        annotations:
          summary: "High API key validation latency"
          description: "P95 validation latency is {{ $value }}ms"

      # High HTTP error rate
      - alert: HighHTTPErrorRate
        expr: |
          rate(http_server_request_duration_count{http_response_status_code=~"5.."}[5m])
          /
          rate(http_server_request_duration_count[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
          service: auth-service
        annotations:
          summary: "High HTTP 5xx error rate"
          description: "Error rate is {{ $value | humanizePercentage }}"
```

## Testing Metrics

### 1. Start the Service

```bash
cd src/Services/MoneyBee.Auth.Service
dotnet run
```

### 2. Access Metrics Endpoint

```bash
curl http://localhost:5001/metrics
```

### 3. Generate Load

```bash
# Create API key
curl -X POST http://localhost:5001/api/apikeys \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Key",
    "description": "Load testing",
    "expiresInDays": 30
  }'

# Validate API key (repeat multiple times)
for i in {1..100}; do
  curl -H "X-API-Key: mb_your_api_key_here" \
    http://localhost:5001/api/apikeys
done
```

### 4. Verify Metrics

Check the `/metrics` endpoint for:
- `auth_apikey_validations_total` counter increments
- `auth_cache_hits_total` increments after first request
- `auth_apikey_validation_duration_bucket` histogram buckets

## Performance Impact

### Overhead

- **Memory**: ~2MB additional for metrics collection
- **CPU**: <1% under normal load
- **Latency**: <0.1ms per metric recording

### Optimization Tips

1. **Cardinality Control**: Limit unique tag values
2. **Sampling**: Use histograms for timing metrics
3. **Aggregation**: Pre-aggregate where possible
4. **Batch Recording**: Record multiple metrics together

## Best Practices

### DO ✅

- Use descriptive metric names following OpenTelemetry conventions
- Include units in metric names (duration, count, bytes)
- Use appropriate metric types (Counter, Histogram, Gauge)
- Add meaningful tags for filtering and grouping
- Document all custom metrics
- Monitor metrics cardinality

### DON'T ❌

- Create high-cardinality metrics (e.g., user IDs as tags)
- Record PII (Personally Identifiable Information) in metrics
- Use string concatenation for metric names
- Create too many custom metrics without justification
- Ignore metric collection errors

## Troubleshooting

### Metrics Not Appearing

1. **Check service registration**:
   ```csharp
   builder.Services.AddSingleton<AuthMetrics>();
   ```

2. **Verify OpenTelemetry configuration**:
   ```csharp
   .AddMeter("MoneyBee.Auth.Service")
   ```

3. **Confirm endpoint mapping**:
   ```csharp
   app.MapPrometheusScrapingEndpoint();
   ```

4. **Check logs for errors**:
   ```bash
   docker logs moneybee-auth-service | grep -i metric
   ```

### High Cardinality Issues

- Limit unique tag values (<1000 per metric)
- Avoid user-specific identifiers in tags
- Use aggregated dimensions

### Memory Issues

- Reduce histogram bucket counts
- Decrease retention period
- Implement metric sampling for high-frequency events

## Migration Path

### From No Metrics → OpenTelemetry

1. ✅ Install OpenTelemetry packages
2. ✅ Create AuthMetrics service
3. ✅ Integrate into existing services
4. ✅ Configure Program.cs
5. ✅ Add Prometheus endpoint
6. ⏭️ Set up Prometheus server
7. ⏭️ Create Grafana dashboards
8. ⏭️ Configure alerting rules

## Resources

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Dashboards](https://grafana.com/grafana/dashboards/)
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)

## Support

For issues or questions:
- Check logs in `docker logs moneybee-auth-service`
- Review metrics at `http://localhost:5001/metrics`
- Verify Prometheus scraping at `http://prometheus:9090/targets`
