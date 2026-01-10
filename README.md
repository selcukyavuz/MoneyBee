# 🐝 MoneyBee - Money Transfer Microservices System

MoneyBee is a modern money transfer system built with microservices architecture. It provides a secure and scalable solution with API key-based authentication, KYC integration, fraud detection, and exchange rate services.

## 🏗️ Architecture

The system consists of 3 main microservices:

### 1. **Auth Service** (Port: 5001)
- API Key management (CRUD operations)
- SHA256-based API Key hashing
- **Redis-based caching** (API key validation, 5-min TTL, 95%+ hit rate)
- Redis-based rate limiting (sliding window, 100 req/min)
- API Key validation endpoint

### 2. **Customer Service** (Port: 5002)
- Customer management (Individual & Corporate)
- Turkish National ID validation
- KYC service integration
- RabbitMQ event publishing for customer status changes
- Age verification (18+ required)

### 3. **Transfer Service** (Port: 5003)
- Money transfer operations
- Multi-currency support (TRY, USD, EUR, GBP)
- Fraud detection integration
- Exchange rate service integration
- Daily limit control (10,000 TRY/customer)
- High-value approval wait (>1000 TRY = 5 minutes)
- Idempotency key support
- Fee calculation (5 TRY base + 1%)
- 8-digit transaction code generation
- Automatic cancellation of pending transfers when customer is blocked

## 🛠️ Technology Stack

- **.NET 8.0** - Framework
- **PostgreSQL 16** - Database (separate DB for each service)
- **Redis 7** - Rate limiting and caching
- **RabbitMQ 3** - Event-driven communication
- **Entity Framework Core 8** - ORM
- **Polly 8** - Resilience patterns (circuit breaker, retry)
- **Serilog** - Structured logging
- **Docker & Docker Compose** - Containerization

## 📋 Requirements

- Docker and Docker Compose
- .NET 8.0 SDK (for local development)
- Postman (for API testing - collection provided)

## 🚀 Setup and Running

### 1. Clone the Repository

```bash
git clone <repository-url>
cd MoneyBee
```

### 2. Start All Services with Docker Compose

```bash
docker-compose up -d
```

This command starts:
- 3x PostgreSQL (Auth, Customer, Transfer DBs)
- 1x Redis
- 1x RabbitMQ
- 3x External Services (Fraud, KYC, Exchange Rate)
- 3x MoneyBee Services (Auth, Customer, Transfer)

### 3. Check Service Status

```bash
docker-compose ps
```

### 4. Health Checks

- Auth Service: http://localhost:5001/health
- Customer Service: http://localhost:5002/health
- Transfer Service: http://localhost:5003/health

### 5. Swagger UI

- Auth Service: http://localhost:5001/swagger
- Customer Service: http://localhost:5002/swagger
- Transfer Service: http://localhost:5003/swagger

### 6. Postman Collection

Import `MoneyBee.postman_collection.json` in the project root:

1. Open Postman
2. Import → File → Select `MoneyBee.postman_collection.json`
3. All endpoints and test scenarios are ready in the collection
4. First, run **"Auth Service → Create API Key"** request
5. API Key is automatically saved to environment variables
6. Run other requests in sequence

Pre-configured scenarios:
- ✅ Complete Transfer Flow (API Key → Customer → Transfer)
- ✅ Rate Limiting Test
- ✅ Daily Limit Test
- ✅ High-Value Transfer Test (>1000 TRY)
- ✅ Multi-Currency Transfer (USD, EUR)

## 📚 API Usage Examples

### 1. Create API Key

```bash
POST http://localhost:5001/api/auth/keys
Content-Type: application/json

{
  "name": "Test Application",
  "description": "API key for testing"
}
```

**⚠️ Important:** API Key is shown only once, save it securely!

### 2. Create Customer

```bash
POST http://localhost:5002/api/customers
Content-Type: application/json
X-API-Key: mbk_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

{
  "nationalId": "12345678901",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "phoneNumber": "+905551234567",
  "dateOfBirth": "1990-01-15",
  "customerType": "Individual"
}
```

### 3. Create Transfer

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
```

### 4. Complete Transfer

```bash
POST http://localhost:5003/api/transfers/12345678/complete
Content-Type: application/json
X-API-Key: mbk_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

{
  "nationalId": "12345678901"
}
```

## 🎯 Key Business Rules

### Rate Limiting
- Maximum **100 requests per minute** per API key
- Implemented with sliding window algorithm in Redis
- Returns `429 Too Many Requests` when limit exceeded

### Daily Transfer Limit
- Each customer can transfer maximum **10,000 TRY** daily
- Transfers in different currencies are converted to TRY

### High-Value Transfer Approval
- Transfers **over 1000 TRY** require **5 minutes** waiting period
- Complete operation cannot be performed before waiting period expires
- Returns `ApprovalWaitRequired` error

### Fraud Detection
- **Low Risk:** Transfer auto-approved
- **Medium Risk:** Transfer stays pending, requires manual approval
- **High Risk:** Transfer automatically rejected

### Customer Blocking
- When customer is blocked:
  - All pending transfers are automatically cancelled
  - Processed in real-time via RabbitMQ event consumer
  - New transfers cannot be created

### Idempotency
- `X-Idempotency-Key` header prevents duplicate transfers
- Returns existing transfer if same key is used
- Use unique key for each different operation

### Fee Calculation
- **Base Fee:** 5 TRY
- **Percentage Fee:** 1% of amount
- **Total Fee:** 5 + (Amount * 0.01)
- Example: 1000 TRY transfer = 5 + 10 = 15 TRY fee

## 🗄️ Database Schema

### Auth Service (auth_db)
- **ApiKeys:** API key management

### Customer Service (customer_db)
- **Customers:** Customer information

### Transfer Service (transfer_db)
- **Transfers:** Transfer transactions

### Migrations
Each service automatically applies migrations at startup.

## 📊 Monitoring & Observability

### Health Checks
Each service reports health status via `/health` endpoint:
- Database connectivity
- Redis connectivity
- RabbitMQ connectivity (Transfer Service)

### Logging
- **Serilog** for structured logging
- JSON format console output
- Request/response logging
- Error tracking

### Resilience
- **Circuit Breaker:** For external services (Fraud, KYC, Exchange Rate)
- **Retry Policy:** Automatic retry for transient failures
- **Timeout:** 10 seconds timeout for each external request

## 🧪 Testing

**Status:** ✅ **79 passing unit tests**

MoneyBee provides comprehensive test coverage:

| Test Suite | Tests | Status |
|------------|-------|--------|
| Auth Service | 29 | ✅ 100% |
| Customer Service | 16 | ✅ 100% |
| Transfer Service | 21 | ✅ 100% (including 3 concurrency tests) |
| Integration Tests | 13 | ✅ 100% |
| **Total** | **79** | **✅** |

**Test Frameworks:**
- **xUnit** - Test framework
- **FluentAssertions** - Readable assertions
- **Moq** - Mocking framework

**Test Coverage:**
- ✅ API Key generation, hashing, masking
- ✅ Turkish National ID validation algorithm
- ✅ Transfer domain business rules
- ✅ Daily limit enforcement with concurrent requests
- ✅ Risk level assessment
- ✅ Approval wait logic
- ✅ Distributed lock behavior

**Run Tests:**
```bash
# All tests
dotnet test

# Specific service
dotnet test tests/MoneyBee.Auth.Service.UnitTests
dotnet test tests/MoneyBee.Customer.Service.UnitTests
dotnet test tests/MoneyBee.Transfer.Service.UnitTests
dotnet test tests/MoneyBee.IntegrationTests
```

**Detailed Documentation:** [tests/README.md](tests/README.md)

## 🛡️ Race Condition & Concurrency Solutions

MoneyBee includes production-grade race condition protections:

### 1. **Redis Distributed Lock**
Prevents race conditions in daily limit checks:
```csharp
var lockKey = $"customer:{customerId}:daily-limit";
await _distributedLock.ExecuteWithLockAsync(lockKey, TimeSpan.FromSeconds(10), async () => {
    var dailyTotal = await _repository.GetDailyTotalAsync(customerId, DateTime.Today);
    ValidateDailyLimit(dailyTotal, amount, DAILY_LIMIT_TRY);
});
```

### 2. **Optimistic Concurrency Control**
RowVersion for transfer updates to detect conflicts:
```sql
ALTER TABLE transfers ADD COLUMN row_version bytea;
```

Automatic retry with exponential backoff:
```csharp
for (int attempt = 0; attempt < 3; attempt++)
{
    try {
        await UpdateTransferAsync(transfer);
        break;
    }
    catch (DbUpdateConcurrencyException) {
        // Retry with exponential backoff
    }
}
```

### 3. **Idempotency Key**
Prevents duplicate transfers:
```csharp
if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
{
    var existing = await GetByIdempotencyKeyAsync(request.IdempotencyKey);
    if (existing != null) return existing; // Return same result
}
```

### 4. **Unit of Work Pattern**
Atomic database + event dispatch:
```csharp
await _unitOfWork.SaveChangesAsync(); // DB save + event dispatch atomic
```

## 🛑 Stopping the System

```bash
# Stop services
docker-compose down

# Remove all volumes (clears database data)
docker-compose down -v
```

## 📝 Development

### Local Development

```bash
# Install dependencies
dotnet restore

# Build project
dotnet build

# Run specific service
cd src/Services/MoneyBee.Auth.Service
dotnet run

# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update
```

### Environment Variables

Configuration for each service is in `appsettings.json`:
- **ConnectionStrings:** PostgreSQL connection details
- **Redis:** Redis connection string
- **RabbitMQ:** RabbitMQ host, username, password
- **ExternalServices:** External service URLs

## 🐛 Troubleshooting

### PostgreSQL Connection Error
```bash
# Check if PostgreSQL container is running
docker-compose ps postgres-auth
docker-compose logs postgres-auth
```

### Redis Connection Error
```bash
# Check if Redis container is running
docker-compose ps redis
docker-compose logs redis
```

### RabbitMQ Connection Error
```bash
# Check if RabbitMQ container is running
docker-compose ps rabbitmq

# Access RabbitMQ Management UI
# http://localhost:15672 (guest/guest)
```

### Migration Error
```bash
# Design-time migrations don't require infrastructure services
# Uses IDesignTimeDbContextFactory for migrations

# Create migration:
cd src/Services/MoneyBee.Transfer.Service
dotnet ef migrations add Add_RowVersion_For_OptimisticConcurrency

# Apply migration:
dotnet ef database update
```

## 🔐 Security

- API Keys are hashed with SHA256 before storage
- Rate limiting provides brute force protection
- Customer sensitive data is not encrypted (for demo purposes)
- HTTPS should be mandatory in production

## 📄 License

This project was developed for case study purposes.

## 👥 Contributors

- Developer: Selçuk Yavuz

---

**Note:** This project was developed for the MoneyBee Developer Case Study and is for educational and evaluation purposes.
