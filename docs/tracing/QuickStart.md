# Distributed Tracing - Quick Start Guide 🚀

OpenTelemetry + Jaeger ile end-to-end request tracking'i hızlıca başlatmak için bu kılavuzu takip edin.

## 🎯 5 Dakikada Başla

### 1. Jaeger'ı Başlat

```bash
cd MoneyBee
docker-compose up -d jaeger
```

### 2. Servisleri Başlat

```bash
# Terminal 1 - Auth Service
cd src/Services/MoneyBee.Auth.Service && dotnet run

# Terminal 2 - Customer Service
cd src/Services/MoneyBee.Customer.Service && dotnet run

# Terminal 3 - Transfer Service
cd src/Services/MoneyBee.Transfer.Service && dotnet run
```

### 3. Test Request Gönder

Postman collection'dan bir transfer oluşturun veya:

```bash
# 1. API Key oluştur
curl -X POST http://localhost:5001/api/auth/keys \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Key","description":"Testing"}'

# 2. Transfer oluştur (end-to-end trace için)
curl -X POST http://localhost:5003/api/transfers \
  -H "X-API-Key: YOUR_KEY_HERE" \
  -H "Content-Type: application/json" \
  -d '{...}'
```

### 4. Jaeger UI'da İncele

**Jaeger UI**: http://localhost:16686

1. Service seç: `MoneyBee.Transfer.Service`
2. **Find Traces** tıkla
3. Bir trace'e tıkla
4. Span timeline'ı incele

## 📊 Ne Göreceksiniz?

### Trace Timeline Örneği:
```
MoneyBee.Transfer.Service: POST /api/transfers    [280ms]
├─ Customer validation (HTTP call)                [45ms]
│  └─ MoneyBee.Customer.Service: GET /api/...     [40ms]
│     └─ EF Core: SELECT * FROM Customers         [10ms]
├─ Fraud check (HTTP call)                        [120ms]
├─ Transfer creation                              [30ms]
│  └─ EF Core: INSERT INTO Transfers              [25ms]
└─ Event publish                                  [5ms]
```

## 🎨 Key Features

✅ **Automatic Instrumentation**: Zero code changes gerekli  
✅ **HTTP Tracking**: API calls otomatik izleniyor  
✅ **Database Queries**: SQL statements görünüyor  
✅ **Exception Tracking**: Error'lar trace'e ekleniyor  
✅ **W3C Trace Context**: Servisler arası otomatik propagation  

## 🔍 Kullanım Senaryoları

### Senaryo 1: Performance Debugging
**Problem**: Transfer çok yavaş  
**Çözüm**: Jaeger'da slow trace'i aç → En uzun span'i bul → Optimize et

### Senaryo 2: Error Analysis
**Problem**: %5 transfer başarısız  
**Çözüm**: `error=true` filtresi → Exception details incele → Root cause bul

### Senaryo 3: Service Dependencies
**Problem**: Hangi servisler birbirine bağlı?  
**Çözüm**: Jaeger UI → Dependencies tab → Graph görüntüle

## 📦 Eklenen Paketler

Tüm servislere eklendi:
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` (1.14.0)
- `OpenTelemetry.Instrumentation.EntityFrameworkCore` (1.0.0-beta.13)

## 🔗 Endpoints

| Service | URL |
|---------|-----|
| **Jaeger UI** | http://localhost:16686 |
| OTLP Collector (gRPC) | http://localhost:4317 |
| OTLP Collector (HTTP) | http://localhost:4318 |

## 📚 Detaylı Dokümantasyon

Daha fazla bilgi için:
- [DistributedTracing.md](DistributedTracing.md) - Tam dokümantasyon
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)

## ✅ Test Sonuçları

```bash
✅ 64/64 test başarılı
✅ Build successful
✅ Jaeger running
✅ All services ready for tracing
```

## 🎯 Next Steps

1. ✅ Distributed Tracing kuruldu
2. ⏭️ Production sampling strategy
3. ⏭️ Custom spans ekle (ihtiyaç halinde)
4. ⏭️ Alerting rules (Jaeger + Prometheus integration)

---

**Hızlı test için**: Jaeger UI'ı aç → Service seç → Find Traces → İncele! 🔍
