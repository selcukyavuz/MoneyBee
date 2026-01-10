using System.Diagnostics.Metrics;

namespace MoneyBee.Transfer.Service.Infrastructure.Metrics;

public class TransferMetrics
{
    private readonly Meter _meter;
    
    // Counters
    private readonly Counter<long> _transferCreatedCounter;
    private readonly Counter<long> _transferCompletedCounter;
    private readonly Counter<long> _transferFailedCounter;
    private readonly Counter<long> _transferCancelledCounter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;
    
    // Histograms
    private readonly Histogram<double> _transferOperationDurationHistogram;
    private readonly Histogram<double> _cacheOperationDurationHistogram;
    private readonly Histogram<double> _transferAmountHistogram;
    
    // Observable Gauges
    private readonly ObservableGauge<int> _activeTransfersGauge;
    private readonly ILogger<TransferMetrics>? _logger;

    public TransferMetrics(IMeterFactory meterFactory, ILogger<TransferMetrics>? logger = null)
    {
        _meter = meterFactory.Create("MoneyBee.Transfer.Service");
        _logger = logger;
        
        // Initialize Counters
        _transferCreatedCounter = _meter.CreateCounter<long>(
            "transfer.created.total",
            description: "Total number of transfers created");
        
        _transferCompletedCounter = _meter.CreateCounter<long>(
            "transfer.completed.total",
            description: "Total number of transfers completed");
        
        _transferFailedCounter = _meter.CreateCounter<long>(
            "transfer.failed.total",
            description: "Total number of transfers failed");
        
        _transferCancelledCounter = _meter.CreateCounter<long>(
            "transfer.cancelled.total",
            description: "Total number of transfers cancelled");
        
        _cacheHitCounter = _meter.CreateCounter<long>(
            "transfer.cache.hits.total",
            description: "Total number of cache hits");
        
        _cacheMissCounter = _meter.CreateCounter<long>(
            "transfer.cache.misses.total",
            description: "Total number of cache misses");
        
        // Initialize Histograms
        _transferOperationDurationHistogram = _meter.CreateHistogram<double>(
            "transfer.operation.duration",
            unit: "ms",
            description: "Duration of transfer operations in milliseconds");
        
        _cacheOperationDurationHistogram = _meter.CreateHistogram<double>(
            "transfer.cache.operation.duration",
            unit: "ms",
            description: "Duration of cache operations in milliseconds");
        
        _transferAmountHistogram = _meter.CreateHistogram<double>(
            "transfer.amount",
            unit: "TRY",
            description: "Transfer amount distribution");
        
        // Initialize Observable Gauges
        _activeTransfersGauge = _meter.CreateObservableGauge<int>(
            "transfer.active.count",
            observeValue: () => GetActiveTransferCount(),
            description: "Current number of active transfers");
    }

    // Counter methods
    public void RecordTransferCreated(decimal amount)
    {
        _transferCreatedCounter.Add(1);
        _transferAmountHistogram.Record((double)amount);
    }
    
    public void RecordTransferCompleted()
    {
        _transferCompletedCounter.Add(1);
    }
    
    public void RecordTransferFailed()
    {
        _transferFailedCounter.Add(1);
    }
    
    public void RecordTransferCancelled()
    {
        _transferCancelledCounter.Add(1);
    }
    
    public void RecordCacheHit()
    {
        _cacheHitCounter.Add(1);
    }
    
    public void RecordCacheMiss()
    {
        _cacheMissCounter.Add(1);
    }
    
    // Histogram methods
    public void RecordTransferOperation(string operation, double durationMs)
    {
        _transferOperationDurationHistogram.Record(durationMs, 
            new KeyValuePair<string, object?>("operation", operation));
    }
    
    public void RecordCacheOperation(string operation, double durationMs)
    {
        _cacheOperationDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation));
    }
    
    // Observable Gauge helper
    private int GetActiveTransferCount()
    {
        // Placeholder - in production, this would query actual count
        return 0;
    }
}
