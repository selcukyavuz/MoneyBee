using System.Diagnostics.Metrics;

namespace MoneyBee.Auth.Service.Infrastructure.Metrics;

/// <summary>
/// Custom metrics for Auth Service monitoring
/// </summary>
public class AuthMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _apiKeyValidationCounter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;
    private readonly Counter<long> _apiKeyCreatedCounter;
    private readonly Counter<long> _apiKeyDeletedCounter;
    private readonly Counter<long> _apiKeyUpdatedCounter;
    private readonly Histogram<double> _validationDuration;
    private readonly Histogram<double> _cacheDuration;

    public AuthMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("MoneyBee.Auth.Service");

        // API Key Validation Metrics
        _apiKeyValidationCounter = _meter.CreateCounter<long>(
            "auth.apikey.validations.total",
            unit: "{validations}",
            description: "Total number of API key validation attempts");

        // Cache Metrics
        _cacheHitCounter = _meter.CreateCounter<long>(
            "auth.cache.hits.total",
            unit: "{hits}",
            description: "Total number of cache hits for API key validation");

        _cacheMissCounter = _meter.CreateCounter<long>(
            "auth.cache.misses.total",
            unit: "{misses}",
            description: "Total number of cache misses for API key validation");

        // API Key Operations
        _apiKeyCreatedCounter = _meter.CreateCounter<long>(
            "auth.apikey.created.total",
            unit: "{created}",
            description: "Total number of API keys created");

        _apiKeyDeletedCounter = _meter.CreateCounter<long>(
            "auth.apikey.deleted.total",
            unit: "{deleted}",
            description: "Total number of API keys deleted");

        _apiKeyUpdatedCounter = _meter.CreateCounter<long>(
            "auth.apikey.updated.total",
            unit: "{updated}",
            description: "Total number of API keys updated");

        // Duration Metrics
        _validationDuration = _meter.CreateHistogram<double>(
            "auth.apikey.validation.duration",
            unit: "ms",
            description: "API key validation duration in milliseconds");

        _cacheDuration = _meter.CreateHistogram<double>(
            "auth.cache.operation.duration",
            unit: "ms",
            description: "Cache operation duration in milliseconds");

        // Gauge for active API keys (observed value)
        _meter.CreateObservableGauge(
            "auth.apikey.active.count",
            observeValue: () => GetActiveApiKeyCount(),
            unit: "{keys}",
            description: "Current number of active API keys");
    }

    // Validation Metrics
    public void RecordValidation(bool isValid, double durationMs)
    {
        _apiKeyValidationCounter.Add(1, new KeyValuePair<string, object?>("result", isValid ? "valid" : "invalid"));
        _validationDuration.Record(durationMs);
    }

    // Cache Metrics
    public void RecordCacheHit()
    {
        _cacheHitCounter.Add(1);
    }

    public void RecordCacheMiss()
    {
        _cacheMissCounter.Add(1);
    }

    public void RecordCacheOperation(string operation, double durationMs)
    {
        _cacheDuration.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
    }

    // API Key Operation Metrics
    public void RecordApiKeyCreated()
    {
        _apiKeyCreatedCounter.Add(1);
    }

    public void RecordApiKeyDeleted()
    {
        _apiKeyDeletedCounter.Add(1);
    }

    public void RecordApiKeyUpdated()
    {
        _apiKeyUpdatedCounter.Add(1);
    }

    // This would typically query a repository or maintain a counter
    // For now, returns 0 (will be implemented properly with repository access)
    private static int GetActiveApiKeyCount()
    {
        // TODO: Implement actual count from repository
        return 0;
    }
}
