# Distributed Tracing - OpenTelemetry + Jaeger 🔍

MoneyBee mikroservislerinde end-to-end request tracking ve performance monitoring için OpenTelemetry Distributed Tracing + Jaeger implementasyonu.

## 📋 Özet

**Tamamlanan İşler:**
- ✅ Jaeger All-in-One container (docker-compose)
- ✅ OpenTelemetry Tracing paketleri (tüm servisler)
- ✅ OTLP Exporter configuration
- ✅ ASP.NET Core instrumentation (HTTP requests)
- ✅ HttpClient instrumentation (external calls)
- ✅ EntityFrameworkCore instrumentation (database queries)
- ✅ Automatic context propagation (W3C Trace Context)
- ✅ Exception tracking
- ✅ Custom tags (client IP, etc.)

## 🎯 Faydaları

### 1. **End-to-End Request Tracking**
- Bir request'in tüm servislerden geçişini görselleştirme
- Hangi servisin ne kadar süre harcadığını anlama
- Service dependencies mapping

### 2. **Performance Profiling**
- Her operasyonun latency'sini ölçme
- Database query performance tracking
- External service call timing
- Bottleneck detection

### 3. **Error Troubleshooting**
- Exception'ların nerede oluştuğunu görme
- Error propagation tracking
- Failed request analysis

### 4. **Service Dependency Visualization**
- Servisler arası dependency grafiği
- Call chain görselleştirme
- Critical path analysis

## 🏗️ Mimari

```
┌──────────────┐
│   Browser    │
│   /Postman   │
└──────┬───────┘
       │ HTTP Request
       ▼
┌──────────────────────────────────────────────┐
│           Transfer Service API                │
│  ┌──────────────────────────────────────┐    │
│  │ ASP.NET Core Instrumentation         │    │
│  │ - HTTP request/response tracking     │    │
│  │ - Exception tracking                 │    │
│  │ - Custom tags (IP, etc.)             │    │
│  └──────────────────────────────────────┘    │
│                                               │
│  ┌──────────────────────────────────────┐    │
│  │ HttpClient Instrumentation           │    │
│  │ - Outgoing HTTP calls                │    │
│  │ - W3C Trace Context propagation      │    │
│  └──────────────────────────────────────┘    │
│                                               │
│  ┌──────────────────────────────────────┐    │
│  │ EF Core Instrumentation              │    │
│  │ - Database queries                   │    │
│  │ - SQL statement logging              │    │
│  └──────────────────────────────────────┘    │
└───────────────┬───────────────────────────────┘
                │ W3C Trace Context Headers
                ▼
        ┌──────────────────┐
        │ Customer Service │  (Same instrumentation)
        └──────────────────┘
                │
                ▼
        ┌──────────────────┐
        │   KYC Service    │  (External)
        └──────────────────┘
                │
                │ OTLP (gRPC)
                ▼
        ┌──────────────────┐
        │      Jaeger      │
        │   (Collector)    │
        └──────────────────┘
                │
                ▼
        ┌──────────────────┐
        │   Jaeger UI      │
        │  localhost:16686 │
        └──────────────────┘
```

## 🚀 Kurulum ve Kullanım

### 1. Jaeger'ı Başlat

```bash
# Docker Compose ile Jaeger başlat
docker-compose up -d jaeger

# Jaeger UI'a eriş
open http://localhost:16686
```

### 2. Servisleri Başlat

```bash
# Auth Service
cd src/Services/MoneyBee.Auth.Service
dotnet run

# Customer Service
cd src/Services/MoneyBee.Customer.Service
dotnet run

# Transfer Service
cd src/Services/MoneyBee.Transfer.Service
dotnet run
```

### 3. Test İsteği Gönder

```bash
# Postman collection'dan bir transfer oluştur
# Veya curl ile:

# 1. API Key oluştur
curl -X POST http://localhost:5001/api/auth/keys \
  -H "Content-Type: application/json" \
  -d '{"name": "Test Key", "description": "For testing"}'

# 2. Customer oluştur
curl -X POST http://localhost:5002/api/customers \
  -H "X-API-Key: YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Ahmet",
    "lastName": "Yılmaz",
    "nationalId": "12345678901",
    "phoneNumber": "+905551234567",
    "dateOfBirth": "1990-01-01",
    "customerType": "Individual"
  }'

# 3. Transfer oluştur (end-to-end trace için)
curl -X POST http://localhost:5003/api/transfers \
  -H "X-API-Key: YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "senderId": "SENDER_ID",
    "receiverId": "RECEIVER_ID",
    "amount": 1000,
    "currency": "TRY"
  }'
```

### 4. Jaeger UI'da Trace'leri Görüntüle

1. **Jaeger UI**: http://localhost:16686
2. **Service seç**: `MoneyBee.Transfer.Service`
3. **Find Traces** tıkla
4. Bir trace'e tıkla

## 📊 Jaeger UI Özellikleri

### Trace Görünümü

```
MoneyBee.Transfer.Service: POST /api/transfers    [280ms]
├─ TransferService.CreateTransferAsync            [250ms]
│  ├─ Customer validation (HTTP)                  [45ms]
│  │  └─ MoneyBee.Customer.Service: GET /api/...  [40ms]
│  │     └─ CustomerService.GetCustomerByIdAsync  [35ms]
│  │        └─ EF Core: SELECT * FROM Customers   [10ms]
│  ├─ Fraud check (HTTP)                          [120ms]
│  │  └─ External: FraudService.CheckRiskAsync    [115ms]
│  ├─ Transfer creation                           [30ms]
│  │  └─ EF Core: INSERT INTO Transfers           [25ms]
│  └─ Event publish                               [5ms]
└─ Response                                       [30ms]
```

### Görüntülenen Bilgiler:
- ✅ **Operation Name**: API endpoint veya method adı
- ✅ **Duration**: Her span'in süresi (ms)
- ✅ **Tags**: HTTP method, status code, client IP, etc.
- ✅ **Logs**: Exception details, custom events
- ✅ **Service Dependencies**: Hangi servisler birbirine bağlı
- ✅ **Critical Path**: En uzun süren operasyonlar

## 🔧 Instrumentation Detayları

### 1. ASP.NET Core Instrumentation

```csharp
.AddAspNetCoreInstrumentation(options =>
{
    options.RecordException = true;  // Exception'ları trace'e ekle
    options.EnrichWithHttpRequest = (activity, request) =>
    {
        // Custom tags ekle
        activity.SetTag("http.client_ip", 
            request.HttpContext.Connection.RemoteIpAddress?.ToString());
    };
})
```

**Tracks:**
- HTTP request/response
- Route templates
- Status codes
- Exception details
- Custom tags (client IP)

### 2. HttpClient Instrumentation

```csharp
.AddHttpClientInstrumentation(options =>
{
    options.RecordException = true;  // HTTP call exception'ları
})
```

**Tracks:**
- Outgoing HTTP requests
- External service calls
- Request/response timing
- HTTP status codes
- W3C Trace Context propagation (automatic)

### 3. EntityFrameworkCore Instrumentation

```csharp
.AddEntityFrameworkCoreInstrumentation(options =>
{
    options.SetDbStatementForText = true;        // SQL query text
    options.SetDbStatementForStoredProcedure = true;
})
```

**Tracks:**
- Database queries
- SQL statements
- Query execution time
- Connection info
- Stored procedure calls

### 4. OTLP Exporter

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:4317");  // Jaeger gRPC endpoint
})
```

**Configuration:**
- Protocol: OTLP over gRPC
- Endpoint: Jaeger collector (port 4317)
- Format: Protobuf
- Batching: Automatic

## 📈 Trace Örnekleri

### Örnek 1: Başarılı Transfer

```
Trace ID: 7f8a3b2c9d1e4f5a6b7c8d9e0f1a2b3c
Duration: 280ms
Spans: 12
Services: 3 (Transfer, Customer, Fraud)

Transfer Service POST /api/transfers              [280ms] ✅
├─ Validate sender (Customer Service)             [45ms] ✅
├─ Validate receiver (Customer Service)           [42ms] ✅
├─ Fraud check                                    [120ms] ✅
├─ DB: Insert transfer                            [25ms] ✅
└─ RabbitMQ: Publish event                        [5ms] ✅
```

### Örnek 2: Failed Transfer (High Risk)

```
Trace ID: 8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d
Duration: 195ms
Spans: 8
Services: 3
Status: ❌ Error

Transfer Service POST /api/transfers              [195ms] ❌
├─ Validate sender                                [40ms] ✅
├─ Validate receiver                              [38ms] ✅
├─ Fraud check                                    [110ms] ⚠️
│  └─ Risk Level: HIGH                            
└─ Exception: FraudDetectionException             
    Message: "High risk transaction detected"
    Stack trace: ...
```

### Örnek 3: Slow Database Query

```
Trace ID: 9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e
Duration: 520ms (SLOW!)
Spans: 15

Transfer Service GET /api/transfers/customer/{id}  [520ms] ⚠️
└─ TransferService.GetTransfersByCustomerAsync    [500ms]
   └─ EF Core Query                               [480ms] 🐌
      SQL: SELECT * FROM Transfers 
           WHERE CustomerId = @p0
           ORDER BY CreatedAt DESC
      Problem: Missing index on CustomerId!
```

## 🎨 Jaeger UI - Use Cases

### 1. Performance Debugging

**Problem**: Transfer oluşturma çok yavaş
**Solution**:
1. Jaeger'da "MoneyBee.Transfer.Service" seç
2. P95 latency'yi filtrele (>500ms)
3. Slow trace'leri incele
4. En uzun span'i bul (fraud check 300ms)
5. Fraud service'i optimize et veya timeout ekle

### 2. Error Analysis

**Problem**: %5 transfer başarısız oluyor
**Solution**:
1. Jaeger'da "error=true" tag'i ile filtrele
2. Failed trace'lere bak
3. Exception details incele
4. Root cause: "Customer blocked" status
5. Customer Service'e status change event'ini kontrol et

### 3. Service Dependency Mapping

**Problem**: Hangi servisler birbirine bağlı?
**Solution**:
1. Jaeger UI → Dependencies tab
2. Service graph görüntüle:
   ```
   Transfer → Customer → KYC
   Transfer → Fraud
   Transfer → Exchange Rate
   Customer → RabbitMQ
   ```
3. Critical path: Transfer → Customer → KYC (en uzun)

### 4. Latency Breakdown

**Problem**: 500ms toplam latency nereden geliyor?
**Solution**:
Trace span duration'larına bak:
- HTTP overhead: 30ms
- Customer validation: 80ms (16%)
- Fraud check: 250ms (50%) ← BOTTLENECK!
- DB operations: 100ms (20%)
- Event publishing: 40ms (8%)

## 🔍 Query Örnekleri (Jaeger UI)

### Service bazlı sorgular:
```
service=MoneyBee.Transfer.Service
```

### Operation bazlı:
```
operation=POST /api/transfers
```

### Duration filtreleri:
```
minDuration=500ms
maxDuration=2s
```

### Tag bazlı:
```
http.status_code=500
error=true
```

### Kombinasyonlar:
```
service=MoneyBee.Transfer.Service 
  AND operation=POST /api/transfers 
  AND minDuration=300ms 
  AND error=true
```

## 📊 Metrics vs Tracing

| Özellik | Metrics (Prometheus) | Tracing (Jaeger) |
|---------|---------------------|------------------|
| **Amaç** | Aggregated statistics | Individual requests |
| **Veri Tipi** | Counter, Gauge, Histogram | Spans, Traces |
| **Kullanım** | "Kaç transfer başarısız?" | "Bu transfer neden başarısız?" |
| **Storage** | Time-series (efficient) | Individual traces (expensive) |
| **Query** | PromQL (aggregation) | Trace ID (specific) |
| **Alerting** | ✅ Built-in | ❌ Not primary use case |
| **Debugging** | ❌ Limited | ✅ Excellent |
| **Production Cost** | Low | Medium-High |

**Best Practice**: Her ikisini birlikte kullan!
- **Metrics**: Genel health monitoring, alerting
- **Tracing**: Specific issue debugging, performance profiling

## 🔗 W3C Trace Context Propagation

OpenTelemetry otomatik olarak W3C Trace Context header'larını propagate eder:

```http
GET /api/customers/123 HTTP/1.1
Host: localhost:5002
X-API-Key: abc123
traceparent: 00-7f8a3b2c9d1e4f5a6b7c8d9e0f1a2b3c-1234567890abcdef-01
tracestate: congo=t61rcWkgMzE
```

**traceparent format:**
```
version-trace_id-parent_span_id-trace_flags
00-7f8a...2b3c-1234...cdef-01
│  │           │              └─ Flags (sampled)
│  │           └─ Parent Span ID (16 hex)
│  └─ Trace ID (32 hex)
└─ Version
```

## 🚀 Production Deployment

### Sampling Strategy

Production'da %100 trace'leme pahalı. Sampling kullan:

```csharp
.WithTracing(tracing => tracing
    .SetSampler(new TraceIdRatioBasedSampler(0.1))  // %10 sampling
    // veya
    .SetSampler(new AlwaysOnSampler())  // Development
    // veya
    .SetSampler(new AlwaysOffSampler()) // Disable
)
```

**Öneriler:**
- **Development**: 100% sampling
- **Staging**: 50% sampling
- **Production**: 5-10% sampling
- **High traffic**: 1% sampling

### Jaeger Backend Options

#### 1. Jaeger All-in-One (Development)
```yaml
jaeger:
  image: jaegertracing/all-in-one:latest
  # Memory storage, UI included
```

#### 2. Jaeger Production (Elasticsearch)
```yaml
jaeger-collector:
  image: jaegertracing/jaeger-collector:latest
  environment:
    SPAN_STORAGE_TYPE: elasticsearch

jaeger-query:
  image: jaegertracing/jaeger-query:latest
  
elasticsearch:
  image: elasticsearch:8.x
```

#### 3. Managed Services
- **AWS X-Ray**: Native OTLP support
- **Google Cloud Trace**: OTLP compatible
- **Azure Monitor**: Application Insights
- **Datadog APM**: OTLP ingestion
- **New Relic**: OTLP support

## 📝 Best Practices

### 1. Span Naming
✅ **Good:**
```
TransferService.CreateTransferAsync
CustomerRepository.GetByIdAsync
```

❌ **Bad:**
```
Method1
DoStuff
Process
```

### 2. Custom Tags
```csharp
using System.Diagnostics;

var activity = Activity.Current;
activity?.SetTag("transfer.amount", amount);
activity?.SetTag("transfer.currency", currency);
activity?.SetTag("customer.id", customerId);
```

### 3. Exception Tracking
```csharp
try
{
    // risky operation
}
catch (Exception ex)
{
    Activity.Current?.RecordException(ex);
    throw;
}
```

### 4. Custom Spans
```csharp
using var activity = _activitySource.StartActivity("ComplexOperation");
activity?.SetTag("operation.type", "batch");

try
{
    // do work
    activity?.SetStatus(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.RecordException(ex);
    throw;
}
```

## 🎯 Key Metrics to Watch

### Trace-based SLIs
```
- P95 latency per service
- Error rate per endpoint
- External service dependency latency
- Database query performance
- Queue/async operation timing
```

### Jaeger Queries
```
# P95 latency
service=MoneyBee.Transfer.Service 
operation=CreateTransferAsync
lookback=1h

# Error rate
error=true
service=MoneyBee.Transfer.Service
lookback=24h

# Slow queries
minDuration=500ms
operation=~.*Repository.*
```

## 📦 Package Versions

```xml
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.14.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.14.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.14.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.13" />
```

## 🔗 Ports

| Service | Port | Description |
|---------|------|-------------|
| Jaeger UI | 16686 | Web UI |
| OTLP gRPC | 4317 | Trace ingestion (gRPC) |
| OTLP HTTP | 4318 | Trace ingestion (HTTP) |
| Jaeger Collector | 14268 | Jaeger native format |
| Jaeger Agent | 6831 (UDP) | Legacy agent protocol |

## 📚 Resources

- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [OTLP Specification](https://opentelemetry.io/docs/specs/otlp/)

## 🎉 Sonuç

**Distributed Tracing başarıyla eklendi!** 🎊

Artık MoneyBee mikroservislerinde:
✅ End-to-end request tracking
✅ Performance profiling
✅ Error debugging
✅ Service dependency mapping
✅ Production-ready observability

**Next Steps:**
1. Servisleri başlat
2. Test request'leri gönder
3. Jaeger UI'da trace'leri incele
4. Performance bottleneck'leri tespit et
5. Production'a deploy et (sampling ile)

**Jaeger UI**: http://localhost:16686 🔍
