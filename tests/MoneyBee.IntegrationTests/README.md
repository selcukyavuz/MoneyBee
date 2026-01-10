# Integration Tests with Testcontainers

MoneyBee projesi için kapsamlı E2E (End-to-End) integration testleri.

## 📋 İçindekiler

- [Genel Bakış](#genel-bakış)
- [Test Altyapısı](#test-altyapısı)
- [Test Senaryoları](#test-senaryoları)
- [Testleri Çalıştırma](#testleri-çalıştırma)
- [Test Stratejisi](#test-stratejisi)

## 🎯 Genel Bakış

Integration testleri, gerçek container'lar kullanarak tüm servislerin entegrasyonunu test eder:

- **PostgreSQL**: Gerçek veritabanı işlemleri
- **Redis**: Gerçek cache işlemleri
- **RabbitMQ**: Gerçek mesajlaşma işlemleri

### Neden Testcontainers?

✅ **Gerçek Ortam**: Mock'lar yerine gerçek bağımlılıklar kullanılır  
✅ **İzole Testler**: Her test çalıştırması temiz container'larla başlar  
✅ **Tutarlı Sonuçlar**: Yerel ve CI/CD ortamlarında aynı davranış  
✅ **Kolay Cleanup**: Testler bitince container'lar otomatik silinir

## 🏗️ Test Altyapısı

### IntegrationTestFactory

Base class tüm testler için ortak altyapı sağlar:

```csharp
public class IntegrationTestFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
```

**Özellikler**:
- PostgreSQL Container (postgres:16-alpine)
- Redis Container (redis:7-alpine)
- RabbitMQ Container (rabbitmq:3-management-alpine)
- In-memory web server (ASP.NET Core TestServer)
- Otomatik connection string override

**Lifecycle**:
1. `InitializeAsync()`: Container'ları başlat (paralel)
2. `ConfigureWebHost()`: Test configuration'ı override et
3. Testler çalıştır
4. `DisposeAsync()`: Container'ları temizle (paralel)

## 📝 Test Senaryoları

### 1. CompleteTransferFlowTests

**Tam Transfer Akışı Testleri**

#### ✅ Test: CompleteTransferFlow_WithValidCustomers_ShouldSucceed

**Senaryo**:
```
1. Auth Service'den API Key al
2. İki customer oluştur (sender + receiver)
3. KYC verification'ı bekle
4. Transfer oluştur (1,000 TRY)
5. Fraud check'i bekle
6. Transfer durumunu kontrol et
```

**Beklenen**:
- Customer'lar `Active` statüsünde
- Transfer `Completed` statüsünde
- Amount: 1,000.00 TRY

#### ⚠️ Test: HighRiskTransfer_ShouldBeRejectedByFraudCheck

**Senaryo**:
```
1. İki customer oluştur
2. Yüksek riskli transfer (50,000 TRY)
3. Fraud service otomatik reject etmeli
```

**Beklenen**:
- Transfer `Failed` statüsünde
- Fraud detection algoritması devreye girdi

#### 🚫 Test: DailyLimitExceeded_ShouldRejectTransfer

**Senaryo**:
```
1. İki customer oluştur
2. 6,000 TRY transfer (başarılı)
3. 6,000 TRY daha transfer (daily limit: 10,000 TRY)
```

**Beklenen**:
- İkinci transfer `400 Bad Request` döner
- Daily limit koruması çalışır

#### 💱 Test: ForeignExchangeTransfer_ShouldConvertToTRY

**Senaryo**:
```
1. İki customer oluştur
2. 100 USD transfer oluştur
3. Exchange rate service çalışır
```

**Beklenen**:
- Currency: USD
- ConvertedAmount > 100 (USD → TRY conversion)

#### 🔒 Test: BlockedCustomer_PendingTransfersShouldBeCancelled

**Senaryo**:
```
1. İki customer oluştur
2. Transfer oluştur
3. Sender customer'ı blokla
4. RabbitMQ event işlenir
```

**Beklenen**:
- Transfer `Cancelled` veya `Failed` statüsünde
- Event-driven cancellation çalıştı

### 2. CustomerServiceTests

**Customer Service CRUD İşlemleri**

#### ✅ Test: CreateCustomer_WithValidData_ShouldSucceed

**Senaryo**:
```
1. API Key al
2. Yeni customer oluştur
```

**Beklenen**:
- 201 Created status
- Customer ID generate edildi
- Tüm alanlar doğru kaydedildi

#### 📖 Test: GetCustomer_ById_ShouldReturnCorrectCustomer

**Senaryo**:
```
1. Customer oluştur
2. ID ile customer'ı getir
```

**Beklenen**:
- 200 OK status
- Doğru customer data

#### ✏️ Test: UpdateCustomer_WithValidData_ShouldSucceed

**Senaryo**:
```
1. Customer oluştur
2. Bilgileri güncelle
```

**Beklenen**:
- 200 OK status
- Güncel data döner

#### 🔒 Test: BlockCustomer_ShouldChangeStatus

**Senaryo**:
```
1. Customer oluştur
2. Customer'ı blokla
```

**Beklenen**:
- Status: `Blocked`

#### 🔓 Test: UnblockCustomer_ShouldChangeStatus

**Senaryo**:
```
1. Customer oluştur ve blokla
2. Customer'ı unblock et
```

**Beklenen**:
- Status: `Active`

#### 🗑️ Test: DeleteCustomer_ShouldRemoveFromDatabase

**Senaryo**:
```
1. Customer oluştur
2. Customer'ı sil
3. Getirmeye çalış
```

**Beklenen**:
- 404 Not Found

#### 📊 Test: GetAllCustomers_ShouldReturnList

**Senaryo**:
```
1. 3 customer oluştur
2. Tüm customer'ları getir
```

**Beklenen**:
- Liste minimum 3 customer içerir

#### ❌ Test: CreateCustomer_WithInvalidEmail_ShouldFail

**Senaryo**:
```
1. Geçersiz email ile customer oluştur
```

**Beklenen**:
- 400 Bad Request
- Validation hatası

## 🚀 Testleri Çalıştırma

### Tüm Testleri Çalıştır

```bash
dotnet test
```

### Sadece Integration Testleri

```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### Belirli Bir Test Class'ı

```bash
dotnet test --filter "FullyQualifiedName~CompleteTransferFlowTests"
```

### Belirli Bir Test

```bash
dotnet test --filter "FullyQualifiedName~CompleteTransferFlow_WithValidCustomers_ShouldSucceed"
```

### Verbose Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

## 📊 Test Stratejisi

### Paralel Execution

Testcontainers her test class'ı için ayrı container'lar oluşturur. Bu sayede testler paralel çalışabilir:

```bash
dotnet test --parallel
```

### Test Süresi

**Beklenen Süreler**:
- Container başlatma: ~10-15 saniye (ilk çalıştırma)
- Container başlatma: ~2-3 saniye (cached image'lar)
- Her test: ~2-5 saniye
- Toplam: ~30-60 saniye (11 test için)

### Container Yönetimi

**Otomatik Cleanup**:
- Testler bitince container'lar otomatik silinir
- `IAsyncLifetime` interface kullanılır
- `xUnit` fixture mekanizması

**Manuel Cleanup** (gerekirse):
```bash
docker ps -a | grep testcontainers | awk '{print $1}' | xargs docker rm -f
```

## 🔧 Troubleshooting

### Problem: Docker daemon'a erişilemiyor

**Çözüm**:
```bash
# macOS / Linux
docker ps

# Eğer hata alıyorsanız, Docker Desktop'ı başlatın
```

### Problem: Port çakışması

**Çözüm**:
Testcontainers otomatik olarak rastgele portlar kullanır. Manuel port açmanıza gerek yok.

### Problem: Testler çok yavaş

**Çözüm**:
```bash
# Image'ları önceden pull edin
docker pull postgres:16-alpine
docker pull redis:7-alpine
docker pull rabbitmq:3-management-alpine
```

### Problem: Container'lar silinmiyor

**Çözüm**:
```bash
# Tüm Testcontainers'ı temizle
docker ps -a --filter "label=org.testcontainers=true" -q | xargs docker rm -f

# Tüm volumes'leri temizle
docker volume ls --filter "label=org.testcontainers=true" -q | xargs docker volume rm
```

## 📈 Gelecek İyileştirmeler

### 1. Performance Tests
- [ ] Load testing (k6)
- [ ] Stress testing
- [ ] Concurrent user testing

### 2. Contract Tests
- [ ] Pact.NET integration
- [ ] API contract verification
- [ ] Message contract testing

### 3. Code Coverage
- [ ] Coverlet integration
- [ ] Coverage reports
- [ ] Minimum coverage threshold

### 4. CI/CD Integration
- [ ] GitHub Actions workflow
- [ ] Automatic test runs
- [ ] Test result artifacts

## 📚 Kaynaklar

- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [xUnit Documentation](https://xunit.net/)
- [ASP.NET Core Integration Tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [FluentAssertions](https://fluentassertions.com/)

## ✅ Test Coverage

**Mevcut Coverage**:
- ✅ Auth Service: API Key generation
- ✅ Customer Service: CRUD operations
- ✅ Transfer Service: Transfer flow
- ✅ Event-Driven: RabbitMQ events
- ✅ Fraud Detection: High-risk scenarios
- ✅ Exchange Rate: Foreign currency
- ✅ Daily Limits: Rate limiting
- ✅ Status Changes: Customer blocking

**Toplam**: 11 E2E test senaryosu

---

**Son Güncelleme**: 2026  
**Test Count**: 11 tests  
**Estimated Duration**: ~60 seconds  
**Success Rate**: ✅ Tüm testler geçiyor
