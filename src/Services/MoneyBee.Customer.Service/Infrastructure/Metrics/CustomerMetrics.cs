using System.Diagnostics.Metrics;

namespace MoneyBee.Customer.Service.Infrastructure.Metrics;

public class CustomerMetrics
{
    private readonly Meter _meter;
    
    // Counters
    private readonly Counter<long> _customerCreatedCounter;
    private readonly Counter<long> _customerDeletedCounter;
    private readonly Counter<long> _customerUpdatedCounter;
    private readonly Counter<long> _kycVerificationCounter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;
    
    // Histograms
    private readonly Histogram<double> _customerOperationDurationHistogram;
    private readonly Histogram<double> _kycVerificationDurationHistogram;
    private readonly Histogram<double> _cacheOperationDurationHistogram;
    
    // Observable Gauges
    private readonly ObservableGauge<int> _activeCustomersGauge;
    private readonly ILogger<CustomerMetrics>? _logger;

    public CustomerMetrics(IMeterFactory meterFactory, ILogger<CustomerMetrics>? logger = null)
    {
        _meter = meterFactory.Create("MoneyBee.Customer.Service");
        _logger = logger;
        
        // Initialize Counters
        _customerCreatedCounter = _meter.CreateCounter<long>(
            "customer.created.total",
            description: "Total number of customers created");
        
        _customerDeletedCounter = _meter.CreateCounter<long>(
            "customer.deleted.total",
            description: "Total number of customers deleted");
        
        _customerUpdatedCounter = _meter.CreateCounter<long>(
            "customer.updated.total",
            description: "Total number of customers updated");
        
        _kycVerificationCounter = _meter.CreateCounter<long>(
            "customer.kyc.verification.total",
            description: "Total number of KYC verifications");
        
        _cacheHitCounter = _meter.CreateCounter<long>(
            "customer.cache.hits.total",
            description: "Total number of cache hits");
        
        _cacheMissCounter = _meter.CreateCounter<long>(
            "customer.cache.misses.total",
            description: "Total number of cache misses");
        
        // Initialize Histograms
        _customerOperationDurationHistogram = _meter.CreateHistogram<double>(
            "customer.operation.duration",
            unit: "ms",
            description: "Duration of customer operations in milliseconds");
        
        _kycVerificationDurationHistogram = _meter.CreateHistogram<double>(
            "customer.kyc.verification.duration",
            unit: "ms",
            description: "Duration of KYC verification in milliseconds");
        
        _cacheOperationDurationHistogram = _meter.CreateHistogram<double>(
            "customer.cache.operation.duration",
            unit: "ms",
            description: "Duration of cache operations in milliseconds");
        
        // Initialize Observable Gauges
        _activeCustomersGauge = _meter.CreateObservableGauge<int>(
            "customer.active.count",
            observeValue: () => GetActiveCustomerCount(),
            description: "Current number of active customers");
    }

    // Counter methods
    public void RecordCustomerCreated()
    {
        _customerCreatedCounter.Add(1);
    }
    
    public void RecordCustomerDeleted()
    {
        _customerDeletedCounter.Add(1);
    }
    
    public void RecordCustomerUpdated()
    {
        _customerUpdatedCounter.Add(1);
    }
    
    public void RecordKycVerification(bool isVerified, double durationMs)
    {
        _kycVerificationCounter.Add(1, 
            new KeyValuePair<string, object?>("result", isVerified ? "verified" : "failed"));
        _kycVerificationDurationHistogram.Record(durationMs);
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
    public void RecordCustomerOperation(string operation, double durationMs)
    {
        _customerOperationDurationHistogram.Record(durationMs, 
            new KeyValuePair<string, object?>("operation", operation));
    }
    
    public void RecordCacheOperation(string operation, double durationMs)
    {
        _cacheOperationDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation));
    }
    
    // Observable Gauge helper - in real scenario, this would query the database or cache
    private int GetActiveCustomerCount()
    {
        // This is a placeholder - in production, this would query the actual count
        // For now, returning 0 to avoid database calls from metrics
        return 0;
    }
}
