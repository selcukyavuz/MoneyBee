# MoneyBee Case Study - Uygulama Planı

## 🏗️ Proje Mimarisi

### Mikroservis Yapısı
```
MoneyBee/
├── docker-compose.yml
├── README.md
├── Services/
│   ├── Auth.Service/          # API Key authentication & rate limiting
│   ├── Customer.Service/      # Müşteri yönetimi + KYC entegrasyonu
│   └── Transfer.Service/      # Para transferi + Fraud + Exchange rate
├── Shared/
│   └── MoneyBee.Common/       # Ortak modeller, helpers, exceptions
└── Postman/
    └── MoneyBee.postman_collection.json
```

## 📋 Detaylı Uygulama Adımları

### 1. Proje Yapısı ve Docker Compose (Gün 1)
- [ ] Solution ve proje yapısını oluştur
- [ ] Docker Compose ile tüm servisleri orkestrasyon
- [ ] PostgreSQL veritabanları (her servis için ayrı DB)
- [ ] Redis (rate limiting için)
- [ ] External servisleri docker-compose'a ekle
- [ ] Shared library oluştur (Common models, DTOs)

### 2. Auth Service (Gün 1-2)
- [ ] API Key bazlı authentication middleware
- [ ] Rate limiting (Redis + Sliding Window)
- [ ] API Key yönetimi endpoints (CRUD)
- [ ] Health check endpoint
- [ ] Swagger documentation

**Endpoints:**
- `POST /api/auth/keys` - Yeni API key oluştur
- `GET /api/auth/keys` - API keyleri listele
- `DELETE /api/auth/keys/{id}` - API key sil

### 3. Customer Service (Gün 2-3)
- [ ] Customer CRUD operasyonları
- [ ] KYC Service entegrasyonu (bpnpay/kyc-service)
- [ ] TC Kimlik No validasyonu algoritması
- [ ] Yaş kontrolü (18+)
- [ ] Customer status yönetimi (Active/Passive/Blocked)
- [ ] Event publishing (customer status changes)
- [ ] Entity Framework Core + PostgreSQL

**Endpoints:**
- `POST /api/customers` - Yeni müşteri kaydı (+ KYC check)
- `GET /api/customers/{id}` - Müşteri detayı
- `GET /api/customers` - Müşteri listesi (pagination)
- `PUT /api/customers/{id}` - Müşteri güncelle
- `PATCH /api/customers/{id}/status` - Status değiştir
- `GET /api/customers/verify/{nationalId}` - Müşteri doğrulama

### 4. Transfer Service (Gün 3-4)
- [ ] Transfer creation logic
- [ ] Fraud Detection Service entegrasyonu
- [ ] Exchange Rate Service entegrasyonu
- [ ] Transaction code generation
- [ ] Daily limit kontrolü (10,000 TRY)
- [ ] Amount > 1000 TRY için 5 dakika bekleme
- [ ] Transaction durumları (PENDING/COMPLETED/CANCELLED/FAILED)
- [ ] Fee hesaplama ve refund logic
- [ ] Idempotency key implementasyonu
- [ ] Background worker (pending transactions için)

**Endpoints:**
- `POST /api/transfers` - Para gönderme
- `POST /api/transfers/{code}/complete` - Para çekme
- `POST /api/transfers/{code}/cancel` - İptal etme
- `GET /api/transfers/{code}` - Transfer detayı
- `GET /api/transfers/customer/{customerId}` - Müşteri transferleri
- `GET /api/transfers/daily-limit/{customerId}` - Günlük limit kontrolü

### 5. Servisler Arası İletişim (Gün 4)
- [ ] HTTP Client factory configuration
- [ ] Circuit breaker pattern (Polly)
- [ ] Retry policies (external services için)
- [ ] Fallback mechanisms
- [ ] Message bus (RabbitMQ/Redis) - customer status events
- [ ] Correlation ID tracking

### 6. API Dokümantasyonu (Gün 5)
- [ ] OpenAPI/Swagger configuration
- [ ] XML documentation comments
- [ ] Postman collection oluşturma
- [ ] Sample requests/responses

### 7. Deployment & Dokümantasyon (Gün 5)
- [ ] README.md (kurulum, çalıştırma)
- [ ] Architecture diagram
- [ ] API kullanım örnekleri
- [ ] Environment variables documentation
- [ ] Docker deployment instructions
- [ ] Troubleshooting guide

## 🛠️ Teknoloji Stack

### Backend
- .NET 8.0 (C#)
- ASP.NET Core Web API
- Entity Framework Core 8
- PostgreSQL

### Infrastructure
- Docker & Docker Compose
- Redis (rate limiting & caching)
- RabbitMQ (event-driven communication)

### Libraries
- Polly (resilience & circuit breaker)
- Serilog (structured logging)
- FluentValidation
- AutoMapper
- MediatR (CQRS pattern - optional)
- Swashbuckle (Swagger)

## 📊 Veritabanı Tasarımı

### Customer Service DB
```sql
Customers:
- Id (UUID)
- FirstName, LastName
- NationalId (unique)
- PhoneNumber
- DateOfBirth
- CustomerType (Individual/Corporate)
- Status (Active/Passive/Blocked)
- KycVerified (bool)
- CreatedAt, UpdatedAt
```

### Transfer Service DB
```sql
Transfers:
- Id (UUID)
- SenderId, ReceiverId (FK to Customer Service)
- Amount, Currency
- AmountInTRY
- TransactionFee
- TransactionCode (unique, 8 digit)
- Status (enum)
- RiskScore
- IdempotencyKey
- CreatedAt, CompletedAt, CancelledAt
```

### Auth Service DB
```sql
ApiKeys:
- Id (UUID)
- Key (hashed)
- Name/Description
- IsActive
- CreatedAt, ExpiresAt
```

## 🔐 Güvenlik ve Best Practices

- ✅ API Key'leri hash'leyerek sakla
- ✅ Rate limiting her endpoint için
- ✅ Input validation (FluentValidation)
- ✅ SQL injection koruması (EF Core)
- ✅ Transaction idempotency
- ✅ Correlation ID için request tracking
- ✅ Structured logging
- ✅ Health checks her servis için
- ✅ Graceful shutdown handling

## 📝 Önemli Noktalar

1. **External Service Resilience:** Circuit breaker ve retry policies mutlaka olmalı
2. **Idempotency:** Aynı transfer 2 kez oluşturulmamalı
3. **Race Conditions:** Daily limit kontrolünde pessimistic locking
4. **Event-Driven:** Customer blocked olduğunda pending transferler iptal edilmeli
5. **Error Handling:** Consistent error response format
6. **Logging:** Her kritik işlem loglanmalı

## 🚀 İlerleme Takibi

### Gün 1: Infrastructure Setup
- [ ] Proje yapısı
- [ ] Docker Compose
- [ ] Auth Service temel yapı

### Gün 2: Customer Service
- [ ] Customer CRUD
- [ ] KYC entegrasyonu
- [ ] Validation logic

### Gün 3-4: Transfer Service
- [ ] Transfer logic
- [ ] Fraud & Exchange rate entegrasyonu
- [ ] Business rules implementation

### Gün 5: Finalization
- [ ] Testing
- [ ] Documentation
- [ ] Deployment
