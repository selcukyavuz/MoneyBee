# Integration Testing - Quick Start

## 🎯 Hızlı Başlangıç

Integration testleri gerçek Docker container'ları kullanır. Test çalıştırmadan önce Docker Desktop'ın çalıştığından emin olun.

## ▶️ Testleri Çalıştır

```bash
# Tüm integration testleri çalıştır
cd /Users/selcukyavuz/repos/MoneyBee
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

## 📊 Mevcut Testler

### CompleteTransferFlowTests (5 test)
1. ✅ `CompleteTransferFlow_WithValidCustomers_ShouldSucceed` - Tam transfer akışı
2. ⚠️ `HighRiskTransfer_ShouldBeRejectedByFraudCheck` - Fraud detection
3. 🚫 `DailyLimitExceeded_ShouldRejectTransfer` - Daily limit kontrolü
4. 💱 `ForeignExchangeTransfer_ShouldConvertToTRY` - Döviz çevrimi
5. 🔒 `BlockedCustomer_PendingTransfersShouldBeCancelled` - Event-driven cancellation

### CustomerServiceTests (6 test)
1. ✅ `CreateCustomer_WithValidData_ShouldSucceed` - Customer oluşturma
2. 📖 `GetCustomer_ById_ShouldReturnCorrectCustomer` - Customer getirme
3. ✏️ `UpdateCustomer_WithValidData_ShouldSucceed` - Customer güncelleme
4. 🔒 `BlockCustomer_ShouldChangeStatus` - Customer bloklama
5. 🔓 `UnblockCustomer_ShouldChangeStatus` - Customer unblock
6. 🗑️ `DeleteCustomer_ShouldRemoveFromDatabase` - Customer silme

**Toplam: 11 E2E Test**

## 🐳 Container'lar

Testler otomatik olarak şu container'ları başlatır:
- **PostgreSQL** (postgres:16-alpine)
- **Redis** (redis:7-alpine)
- **RabbitMQ** (rabbitmq:3-management-alpine)

Container'lar test bitince otomatik silinir.

## ⏱️ Beklenen Süre

- İlk çalıştırma: ~30-45 saniye (image pull + container start)
- Sonraki çalıştırmalar: ~15-20 saniye
- Her test: ~2-5 saniye

## 📁 Dosya Yapısı

```
tests/MoneyBee.IntegrationTests/
├── E2E/
│   ├── CompleteTransferFlowTests.cs    # Transfer flow testleri
│   └── CustomerServiceTests.cs          # Customer CRUD testleri
├── Infrastructure/
│   └── IntegrationTestFactory.cs        # Test base class (Testcontainers)
├── Shared/
│   └── TestDtos.cs                      # Test için DTO'lar
├── README.md                            # Detaylı dokümantasyon
└── QUICKSTART.md                        # Bu dosya
```

## 🔧 Gereksinimler

- [x] .NET 8.0 SDK
- [x] Docker Desktop (running)
- [x] 4GB+ RAM (container'lar için)

## 💡 İpuçları

### Belirli Bir Test Çalıştır

```bash
dotnet test --filter "FullyQualifiedName~CompleteTransferFlow_WithValidCustomers"
```

### Verbose Output

```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests" --logger "console;verbosity=detailed"
```

### Container Temizliği

Eğer testler beklenmedik şekilde durduysa:

```bash
# Tüm testcontainers'ı temizle
docker ps -a --filter "label=org.testcontainers=true" -q | xargs docker rm -f
```

## 🎨 Test Yazma Örneği

```csharp
[Fact]
public async Task YourTest_Scenario_ExpectedBehavior()
{
    // ARRANGE: Test verilerini hazırla
    var apiKey = await GetApiKeyAsync();
    _client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    
    // ACT: İşlemi gerçekleştir
    var response = await _client.PostAsJsonAsync("/api/customers", request);
    
    // ASSERT: Sonucu doğrula
    response.EnsureSuccessStatusCode();
    var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
    customer.Should().NotBeNull();
}
```

## 📚 Daha Fazla Bilgi

Detaylı dokümantasyon için: [README.md](README.md)

---

**Test Count**: 11 tests  
**Technology**: Testcontainers + xUnit + FluentAssertions  
**Status**: ✅ Ready to use
