# 🐝 MoneyBee - Money Transfer Microservices System

MoneyBee, modern mikroservis mimarisi ile geliştirilmiş bir para transferi sistemidir. API key tabanlı kimlik doğrulama, KYC entegrasyonu, fraud detection ve exchange rate servisleri ile güvenli ve ölçeklenebilir bir çözüm sunar.

## 🏗️ Mimari

Sistem 3 ana microservice'ten oluşmaktadır:

### 1. **Auth Service** (Port: 5001)
- API Key yönetimi (create, read, update, delete)
- SHA256 ile API Key hashleme
- Redis tabanlı rate limiting (sliding window, 100 req/min)
- API Key validation endpoint

### 2. **Customer Service** (Port: 5002)
- Müşteri yönetimi (Individual & Corporate)
- TC Kimlik No validasyon
- KYC servis entegrasyonu
- Customer status değişikliklerinde RabbitMQ event publishing
- Yaş kontrolü (18+ zorunlu)

### 3. **Transfer Service** (Port: 5003)
- Para transfer işlemleri
- Multi-currency desteği (TRY, USD, EUR, GBP)
- Fraud detection entegrasyonu
- Exchange rate servisi entegrasyonu
- Daily limit kontrolü (10,000 TRY/customer)
- High-value approval wait (>1000 TRY = 5 dakika bekleme)
- Idempotency key desteği
- Fee hesaplama (5 TRY base + %1)
- 8 haneli transaction code üretimi
- Customer blocked olduğunda pending transferleri otomatik iptal

## 🛠️ Teknoloji Stack

- **.NET 8.0** - Framework
- **PostgreSQL 16** - Database (her servis için ayrı)
- **Redis 7** - Rate limiting ve caching
- **RabbitMQ 3** - Event-driven communication
- **Entity Framework Core 8** - ORM
- **Polly 8** - Resilience patterns (circuit breaker, retry)
- **Serilog** - Structured logging
- **Docker & Docker Compose** - Containerization

## 📋 Gereksinimler

- Docker ve Docker Compose
- .NET 8.0 SDK (local development için)
- Postman (API testing için)

## 🚀 Kurulum ve Çalıştırma

### 1. Repository'yi Klonlayın

```bash
git clone <repository-url>
cd MoneyBee
```

### 2. Docker Compose ile Tüm Sistemi Başlatın

```bash
docker-compose up -d
```

Bu komut aşağıdaki servisleri başlatır:
- 3x PostgreSQL (Auth, Customer, Transfer DB'leri)
- 1x Redis
- 1x RabbitMQ
- 3x External Services (Fraud, KYC, Exchange Rate)
- 3x MoneyBee Services (Auth, Customer, Transfer)

### 3. Servislerin Durumunu Kontrol Edin

```bash
docker-compose ps
```

### 4. Servis Health Check'leri

- Auth Service: http://localhost:5001/health
- Customer Service: http://localhost:5002/health
- Transfer Service: http://localhost:5003/health

### 5. Swagger UI

- Auth Service: http://localhost:5001/swagger
- Customer Service: http://localhost:5002/swagger
- Transfer Service: http://localhost:5003/swagger

## 📚 API Kullanımı

### 1. API Key Oluşturma

```bash
POST http://localhost:5001/api/auth/keys
Content-Type: application/json

{
  "name": "Test Application",
  "description": "Test için API key"
}

Response:
{
  "success": true,
  "data": {
    "id": "guid",
    "name": "Test Application",
    "key": "mbk_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "hashedKey": "...",
    "isActive": true,
    "requestCount": 0,
    "createdAt": "2025-01-09T..."
  }
}
```

**⚠️ Önemli:** API Key sadece bir kez gösterilir, güvenli bir yere kaydedin!

### 2. Müşteri Oluşturma

```bash
POST http://localhost:5002/api/customers
Content-Type: application/json
X-API-Key: mbk_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

{
  "nationalId": "12345678901",
  "firstName": "Ahmet",
  "lastName": "Yılmaz",
  "email": "ahmet@example.com",
  "phoneNumber": "+905551234567",
  "dateOfBirth": "1990-01-15",
  "customerType": "Individual"
}

Response:
{
  "success": true,
  "data": {
    "id": "guid",
    "nationalId": "12345678901",
    "firstName": "Ahmet",
    "lastName": "Yılmaz",
    "email": "ahmet@example.com",
    "status": "Active",
    "kycStatus": "Verified",
    "createdAt": "2025-01-09T..."
  }
}
```

### 3. Para Transferi Oluşturma

```bash
POST http://localhost:5003/api/transfers
Content-Type: application/json
X-API-Key: mbk_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
X-Idempotency-Key: unique-key-123

{
  "senderNationalId": "12345678901",
  "receiverNationalId": "98765432109",
  "amount": 500,
  "currency": "TRY",
  "description": "Test transfer"
}

Response:
{
  "success": true,
  "data": {
    "id": "guid",
    "transactionCode": "12345678",
    "senderId": "guid",
    "receiverId": "guid",
    "amount": 500,
    "currency": "TRY",
    "amountInTRY": 500,
    "transactionFee": 10,
    "status": "Pending",
    "riskLevel": "Low",
    "createdAt": "2025-01-09T..."
  }
}
```

### 4. Transfer Onaylama

```bash
POST http://localhost:5003/api/transfers/12345678/complete
Content-Type: application/json
X-API-Key: mbk_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

{
  "nationalId": "12345678901"
}

Response:
{
  "success": true,
  "data": {
    "transactionCode": "12345678",
    "status": "Completed",
    "completedAt": "2025-01-09T..."
  }
}
```

## 🎯 Önemli İş Kuralları

### Rate Limiting
- Her API key için dakikada maksimum **100 request**
- Sliding window algoritması ile Redis üzerinde tutulur
- Limit aşıldığında `429 Too Many Requests` hatası döner

### Daily Transfer Limit
- Her müşteri günlük maksimum **10,000 TRY** transfer yapabilir
- Farklı para birimlerindeki transferler TRY'ye çevrilerek hesaplanır

### High-Value Transfer Approval
- **1000 TRY üzeri** transferler için **5 dakika** bekleme süresi
- Bekleme süresi dolmadan complete işlemi yapılamaz
- `ApprovalWaitRequired` hatası döner

### Fraud Detection
- **Low Risk:** Transfer otomatik onaylanır
- **Medium Risk:** Transfer pending durumunda kalır, manuel onay gerekir
- **High Risk:** Transfer otomatik olarak reddedilir

### Customer Blocking
- Müşteri blocked duruma alındığında:
  - Pending durumundaki tüm transferleri otomatik iptal edilir
  - RabbitMQ event consumer ile gerçek zamanlı işlenir
  - Yeni transfer oluşturulamaz

### Idempotency
- `X-Idempotency-Key` header'ı ile duplicate transfer önlenir
- Aynı key ile tekrar istek atılırsa mevcut transfer döner
- Her farklı işlem için unique key kullanılmalı

### Fee Calculation
- **Base Fee:** 5 TRY
- **Percentage Fee:** Amount'ın %1'i
- **Total Fee:** 5 + (Amount * 0.01)
- Örnek: 1000 TRY transfer = 5 + 10 = 15 TRY fee

## 🗄️ Database Schema

### Auth Service (auth_db)
- **ApiKeys:** API key yönetimi

### Customer Service (customer_db)
- **Customers:** Müşteri bilgileri

### Transfer Service (transfer_db)
- **Transfers:** Transfer işlemleri

### Migrations
Her servis startup'ta otomatik olarak migration'ları uygular.

## 📊 Monitoring & Observability

### Health Checks
Her servis `/health` endpoint'i üzerinden sağlık durumunu raporlar:
- Database connectivity
- Redis connectivity
- RabbitMQ connectivity (Transfer Service)

### Logging
- **Serilog** ile structured logging
- JSON formatında konsol çıktısı
- Request/response logging
- Error tracking

### Resilience
- **Circuit Breaker:** External service'ler için (Fraud, KYC, Exchange Rate)
- **Retry Policy:** Geçici hatalar için otomatik tekrar deneme
- **Timeout:** Her external request için 10 saniye timeout

## 🧪 Testing

### Manual Testing
1. Postman collection'ı import edin (yakında eklenecek)
2. Environment variables'ları ayarlayın
3. Happy path ve edge case'leri test edin

### Integration Testing
```bash
# Tüm servisleri başlat
docker-compose up -d

# Health check'leri kontrol et
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost:5003/health
```

## 🛑 Sistemi Durdurma

```bash
# Servisleri durdur
docker-compose down

# Tüm volume'leri de sil (database verilerini temizler)
docker-compose down -v
```

## 📝 Development

### Local Development

```bash
# Dependencies yükle
dotnet restore

# Projeyi build et
dotnet build

# Spesifik bir servisi çalıştır
cd src/Services/MoneyBee.Auth.Service
dotnet run

# Migration oluştur
dotnet ef migrations add MigrationName

# Migration uygula
dotnet ef database update
```

### Environment Variables

Her servis için `appsettings.json` dosyasında configuration bulunur:
- **ConnectionStrings:** PostgreSQL bağlantı bilgileri
- **Redis:** Redis connection string
- **RabbitMQ:** RabbitMQ host, username, password
- **ExternalServices:** External service URL'leri

## 🐛 Troubleshooting

### PostgreSQL Connection Error
```bash
# PostgreSQL container'ının çalıştığını kontrol edin
docker-compose ps postgres-auth
docker-compose logs postgres-auth
```

### Redis Connection Error
```bash
# Redis container'ının çalıştığını kontrol edin
docker-compose ps redis
docker-compose logs redis
```

### RabbitMQ Connection Error
```bash
# RabbitMQ container'ının çalıştığını kontrol edin
docker-compose ps rabbitmq

# RabbitMQ Management UI'a erişin
# http://localhost:15672 (guest/guest)
```

### Migration Error
```bash
# Design-time migrations için infrastructure servislerinin çalışması gerekmez
# IDesignTimeDbContextFactory ile migration oluşturulur

# Migration oluşturma:
cd src/Services/MoneyBee.Transfer.Service
dotnet ef migrations add Add_RowVersion_For_OptimisticConcurrency

# Migration çalıştırma:
dotnet ef database update
```

## 🛡️ Race Condition & Concurrency Solutions

MoneyBee, production-grade race condition korumaları içerir:

### 1. **Redis Distributed Lock**
Daily limit kontrollerinde race condition önleme:
```csharp
var lockKey = $"customer:{customerId}:daily-limit";
await _distributedLock.ExecuteWithLockAsync(lockKey, TimeSpan.FromSeconds(10), async () => {
    var dailyTotal = await _repository.GetDailyTotalAsync(customerId, DateTime.Today);
    ValidateDailyLimit(dailyTotal, amount, DAILY_LIMIT_TRY);
});
```

### 2. **Optimistic Concurrency Control**
Transfer update'lerinde RowVersion ile çakışma tespiti:
```sql
ALTER TABLE transfers ADD COLUMN row_version bytea;
```

Retry logic ile otomatik çözüm:
```csharp
for (int attempt = 0; attempt < 3; attempt++)
{
    try {
        await UpdateTransferAsync(transfer);
        break;
    }
    catch (DbUpdateConcurrencyException) {
        // Exponential backoff ile retry
    }
}
```

### 3. **Idempotency Key**
Duplicate transfer önleme:
```csharp
if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
{
    var existing = await GetByIdempotencyKeyAsync(request.IdempotencyKey);
    if (existing != null) return existing; // Same result
}
```

### 4. **Unit of Work Pattern**
Atomik database + event dispatch:
```csharp
await _unitOfWork.SaveChangesAsync(); // DB save + event dispatch atomic
```

**Detaylı Bilgi:**
- [docs/RaceConditionsAndConcurrency.md](docs/RaceConditionsAndConcurrency.md) - Detaylı analiz
- [docs/RaceConditionImprovements.md](docs/RaceConditionImprovements.md) - Implementation guide
- [docs/TroubleshootingAndSolutions.md](docs/TroubleshootingAndSolutions.md) - Sorun giderme
- [RACE_CONDITION_FIXES.md](RACE_CONDITION_FIXES.md) - Quick reference

## 📖 API Documentation

Her servis Swagger UI ile API documentation sağlar:
- Auth Service: http://localhost:5001/swagger
- Customer Service: http://localhost:5002/swagger
- Transfer Service: http://localhost:5003/swagger

## 🔐 Security

- API Key'ler SHA256 ile hashlenip saklanır
- Rate limiting ile brute force koruması
- Customer sensitive bilgileri encrypted değil (demo amaçlı)
- Production'da HTTPS zorunlu olmalı

## 📄 License

Bu proje case study amaçlı geliştirilmiştir.

## 👥 Contributors

- Developer: [Selçuk Yavuz]

## 📞 İletişim

Sorularınız için GitHub Issues kullanabilirsiniz.

---

**Not:** Bu proje MoneyBee Developer Case Study için geliştirilmiştir ve production-ready değildir. Eğitim ve değerlendirme amaçlıdır.
