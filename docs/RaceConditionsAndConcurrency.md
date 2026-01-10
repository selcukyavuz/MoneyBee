# Race Conditions & Concurrency Handling

## Overview

MoneyBee sisteminde race condition'lar ve concurrency sorunları kritik öneme sahiptir. Özellikle:
- Aynı müşteri için eşzamanlı transfer oluşturma
- Günlük limit kontrolü
- Customer status değişiklikleri
- Transaction code generation

Bu döküman mevcut önlemleri ve gelecek iyileştirmeleri açıklar.

## 1. Idempotency (✅ Implemented)

### Problem
Aynı transfer isteği ağ hataları veya retry mekanizmaları nedeniyle birden fazla kez gelebilir.

### Solution
**Idempotency Key** mekanizması ile duplicate request'leri önlüyoruz.

```csharp
public async Task<CreateTransferResponse> CreateTransferAsync(CreateTransferRequest request)
{
    // Check idempotency
    if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
    {
        var existingTransfer = await _repository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
        if (existingTransfer != null)
        {
            _logger.LogInformation("Idempotent request detected: {IdempotencyKey}", 
                request.IdempotencyKey);
            return MapToCreateResponse(existingTransfer);
        }
    }
    
    // Continue with transfer creation...
}
```

### Database Constraints

```csharp
// TransferDbContext.cs
entity.HasIndex(e => e.IdempotencyKey)
    .IsUnique()
    .HasFilter("idempotency_key IS NOT NULL");
```

### API Usage

```http
POST /api/transfers
Content-Type: application/json

{
  "senderId": "uuid",
  "receiverId": "uuid",
  "amount": 100,
  "currency": "TRY",
  "idempotencyKey": "unique-client-generated-key-123"
}
```

**Best Practices:**
- Client generates unique key (UUID recommended)
- Same key returns same result (idempotent)
- Prevents duplicate charges
- Essential for retry logic

## 2. Unique Constraints (✅ Implemented)

### Transaction Code

```csharp
entity.HasIndex(e => e.TransactionCode)
    .IsUnique();
```

Aynı transaction code'un birden fazla kez oluşturulmasını database seviyesinde önler.

### Idempotency Key

```csharp
entity.HasIndex(e => e.IdempotencyKey)
    .IsUnique()
    .HasFilter("idempotency_key IS NOT NULL");
```

NULL değerlere izin verirken, var olan değerlerin unique olmasını garanti eder.

## 3. Daily Limit Race Condition (⚠️ Potential Issue)

### Problem

İki concurrent request aynı anda günlük limit kontrolü yapabilir:

```
Time    Request 1           Request 2           Database
----    ---------           ---------           --------
T1      Read: 9,500 TRY     -                   9,500 TRY
T2      Check: 9,500+600    -                   9,500 TRY
        = 10,100 > 10,000   
        ❌ REJECT
T3      -                   Read: 9,500 TRY     9,500 TRY
T4      -                   Check: 9,500+600    9,500 TRY
                            = 10,100 > 10,000
                            ❌ REJECT
T5      ✅ Both rejected

But if Request 2 was 500 TRY:
T1      Read: 9,500 TRY     -                   9,500 TRY
T2      Check: 9,500+600    Read: 9,500 TRY     9,500 TRY
        = 10,100 > 10,000
T3      ❌ REJECT           Check: 9,500+500    9,500 TRY
                            = 10,000 <= 10,000
T4      -                   ✅ APPROVE          9,500 TRY
T5      -                   Write: 10,000 TRY   10,000 TRY

Correct behavior ✅

But with concurrent 500 TRY requests:
T1      Read: 9,500 TRY     Read: 9,500 TRY     9,500 TRY
T2      Check: OK           Check: OK           9,500 TRY
T3      Write: 10,000       Write: 10,000       10,000 TRY
T4      ✅ APPROVE          ✅ APPROVE          10,000 TRY (WRONG!)

Result: 10,500 TRY processed (exceeds limit) ❌
```

### Current Implementation

```csharp
// TransferService.cs
var dailyTotal = await _repository.GetDailyTotalAsync(sender.Id, DateTime.Today);
_domainService.ValidateDailyLimit(dailyTotal, amountInTRY, DAILY_LIMIT_TRY);

// TransferDomainService.cs
public void ValidateDailyLimit(decimal currentTotal, decimal newAmount, decimal limit)
{
    if (currentTotal + newAmount > limit)
    {
        throw new InvalidOperationException(
            $"Daily limit exceeded. Current: {currentTotal} TRY, Requested: {newAmount} TRY, Limit: {limit} TRY");
    }
}
```

**Issue:** Read → Check → Write pattern is NOT atomic.

### Solutions

#### Option 1: Database Row-Level Locking (Pessimistic) ⭐ Recommended

```csharp
public async Task<CreateTransferResponse> CreateTransferAsync(CreateTransferRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // Lock customer row for update
        var sender = await _context.Customers
            .FromSqlRaw("SELECT * FROM customers WHERE id = {0} FOR UPDATE", request.SenderId)
            .FirstOrDefaultAsync();
        
        // Now safely calculate daily total
        var dailyTotal = await _repository.GetDailyTotalAsync(sender.Id, DateTime.Today);
        _domainService.ValidateDailyLimit(dailyTotal, amountInTRY, DAILY_LIMIT_TRY);
        
        // Create transfer
        var transfer = TransferEntity.Create(...);
        await _repository.CreateAsync(transfer);
        
        await transaction.CommitAsync();
        return MapToCreateResponse(transfer);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Pros:**
- Guarantees consistency
- Simple to implement
- Works in single-database setup

**Cons:**
- Reduces throughput (locks)
- Potential deadlocks
- Doesn't scale to distributed systems

#### Option 2: Distributed Lock with Redis ⭐⭐ Best for Microservices

```csharp
public class DistributedLockService
{
    private readonly IConnectionMultiplexer _redis;
    
    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        return await db.StringSetAsync(key, "locked", expiry, When.NotExists);
    }
    
    public async Task ReleaseLockAsync(string key)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
}

// Usage in TransferService
public async Task<CreateTransferResponse> CreateTransferAsync(CreateTransferRequest request)
{
    var lockKey = $"transfer:daily-limit:{sender.Id}";
    var lockAcquired = await _lockService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10));
    
    if (!lockAcquired)
    {
        throw new InvalidOperationException("Another transfer is being processed. Please try again.");
    }
    
    try
    {
        var dailyTotal = await _repository.GetDailyTotalAsync(sender.Id, DateTime.Today);
        _domainService.ValidateDailyLimit(dailyTotal, amountInTRY, DAILY_LIMIT_TRY);
        
        var transfer = TransferEntity.Create(...);
        await _repository.CreateAsync(transfer);
        
        return MapToCreateResponse(transfer);
    }
    finally
    {
        await _lockService.ReleaseLockAsync(lockKey);
    }
}
```

**Pros:**
- Works across multiple instances
- Scales horizontally
- Industry standard (Redis SETNX)

**Cons:**
- Requires Redis
- Lock timeout handling needed
- More complex

#### Option 3: Optimistic Concurrency with RowVersion

```csharp
// Customer Entity
public class Customer : AggregateRoot
{
    [Timestamp]
    public byte[] RowVersion { get; set; }
    
    public decimal DailyTransferTotal { get; private set; }
    
    public void AddToDailyTotal(decimal amount)
    {
        DailyTransferTotal += amount;
    }
}

// TransferService
try
{
    sender.AddToDailyTotal(amountInTRY);
    await _customerRepository.UpdateAsync(sender);
}
catch (DbUpdateConcurrencyException)
{
    // Retry logic
    throw new InvalidOperationException("Concurrent update detected. Please retry.");
}
```

**Pros:**
- No locks (better performance)
- Detection of conflicts
- Works well with EF Core

**Cons:**
- Requires application retry logic
- User sees errors
- Not intuitive for developers

## 4. Transaction Code Generation (✅ Safe with Unique Constraint)

### Current Implementation

```csharp
public static string Generate()
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    var random = new Random();
    return new string(Enumerable.Repeat(chars, 10)
        .Select(s => s[random.Next(s.Length)]).ToArray());
}
```

### Race Condition Safety

Database unique constraint garantisi altında:

```csharp
while (true)
{
    try
    {
        var code = await GenerateUniqueTransactionCodeAsync();
        transfer.TransactionCode = code;
        await _repository.CreateAsync(transfer);
        break;
    }
    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
    {
        // Retry with new code
        _logger.LogWarning("Transaction code collision detected. Retrying...");
        continue;
    }
}
```

**Better Approach:** Check before insert

```csharp
private async Task<string> GenerateUniqueTransactionCodeAsync()
{
    string code;
    bool exists;
    
    do
    {
        code = TransactionCodeGenerator.Generate();
        exists = await _repository.TransactionCodeExistsAsync(code);
    } while (exists);
    
    return code;
}
```

## 5. Customer Status Changes & Concurrent Transfers

### Problem

```
Time    Status Change       Transfer Create     Database
----    -------------       ---------------     --------
T1      Block Customer      -                   Active
T2      -                   Check Status: ✅    Active
T3      Update: Blocked     -                   Blocked
T4      -                   Create Transfer ✅  Blocked (WRONG!)

Result: Transfer created for blocked customer ❌
```

### Solution 1: Read Customer Status in Transaction

```csharp
public async Task<CreateTransferResponse> CreateTransferAsync(CreateTransferRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // Read sender within transaction
        var sender = await _customerService.VerifyCustomerAsync(request.SenderId);
        
        if (sender.Status == CustomerStatus.Blocked)
        {
            throw new InvalidOperationException("Sender is blocked");
        }
        
        // Create transfer
        var transfer = TransferEntity.Create(...);
        await _repository.CreateAsync(transfer);
        
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Solution 2: Event-Driven Cancellation (✅ Current)

```csharp
// CustomerStatusChangedEvent handler in Transfer Service
public async Task HandleCustomerStatusChangedAsync(CustomerStatusChangedEvent customerEvent)
{
    if (customerEvent.NewStatus == CustomerStatus.Blocked.ToString())
    {
        var pendingTransfers = await _repository.GetPendingTransfersByCustomerAsync(
            customerEvent.CustomerId);
        
        foreach (var transfer in pendingTransfers)
        {
            transfer.Cancel($"Customer {customerEvent.CustomerId} was blocked");
            await _repository.UpdateAsync(transfer);
        }
    }
}
```

**Eventual Consistency:** Transfer may be created and then cancelled within seconds.

## 6. Optimistic Concurrency Control (❌ Not Implemented)

### What is it?

Versiyon kontrolü ile concurrent update'leri tespit etme:

```csharp
public class Transfer : AggregateRoot
{
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```

### How it Works

```
User 1: Read Transfer (RowVersion = 1)
User 2: Read Transfer (RowVersion = 1)
User 1: Update Transfer (RowVersion = 1 → 2) ✅
User 2: Update Transfer (RowVersion = 1 → 2) ❌ Conflict!
```

### Implementation

```csharp
// Entity
public class Transfer : AggregateRoot
{
    [Timestamp]
    public byte[] RowVersion { get; set; }
}

// Service
try
{
    transfer.Complete();
    await _repository.UpdateAsync(transfer);
}
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning("Concurrent update detected for transfer {TransferId}", transfer.Id);
    throw new ConcurrencyException("Transfer was modified by another user. Please refresh.");
}
```

## 7. Unit of Work Pattern (❌ Not Implemented)

### Problem

Repository ve event dispatcher ayrı transaction'larda:

```csharp
await _repository.CreateAsync(transfer);  // Transaction 1
await _domainEventDispatcher.DispatchAsync(transfer.DomainEvents);  // No transaction
```

**Issue:** Event dispatch fails → transfer saved but events not published.

### Solution: Unit of Work

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly TransferDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Get all aggregates with events
        var aggregates = _context.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();
        
        // Save to database
        var result = await _context.SaveChangesAsync(cancellationToken);
        
        // Dispatch events AFTER successful save
        foreach (var aggregate in aggregates)
        {
            await _eventDispatcher.DispatchAsync(aggregate.DomainEvents);
            aggregate.ClearDomainEvents();
        }
        
        return result;
    }
}

// Usage
public class TransferService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<CreateTransferResponse> CreateTransferAsync(CreateTransferRequest request)
    {
        var transfer = TransferEntity.Create(...);
        
        _context.Transfers.Add(transfer);
        
        // Atomically save and dispatch events
        await _unitOfWork.SaveChangesAsync();
        
        return MapToCreateResponse(transfer);
    }
}
```

## 8. Outbox Pattern (❌ Not Implemented)

### Problem

Domain event dispatch fails → data saved but events not published.

### Solution

Event'leri önce DB'ye yaz, sonra async olarak publish et:

```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string EventData { get; set; }  // JSON
    public DateTime OccurredOn { get; set; }
    public bool Published { get; set; }
    public DateTime? PublishedAt { get; set; }
}

// Service
public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
{
    var aggregates = GetAggregatesWithEvents();
    
    // Save outbox messages in same transaction
    foreach (var aggregate in aggregates)
    {
        foreach (var domainEvent in aggregate.DomainEvents)
        {
            var outboxMessage = new OutboxMessage
            {
                EventType = domainEvent.GetType().Name,
                EventData = JsonSerializer.Serialize(domainEvent),
                OccurredOn = domainEvent.OccurredOn
            };
            
            _context.OutboxMessages.Add(outboxMessage);
        }
    }
    
    // Atomic: Data + Outbox
    return await _context.SaveChangesAsync(cancellationToken);
}

// Background Service
public class OutboxProcessor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var unpublishedMessages = await _context.OutboxMessages
                .Where(m => !m.Published)
                .OrderBy(m => m.OccurredOn)
                .Take(100)
                .ToListAsync();
            
            foreach (var message in unpublishedMessages)
            {
                try
                {
                    await PublishToRabbitMQAsync(message);
                    
                    message.Published = true;
                    message.PublishedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                }
            }
            
            await _context.SaveChangesAsync();
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

## 9. Summary Table

| Race Condition | Current Status | Severity | Solution Priority |
|----------------|----------------|----------|-------------------|
| Duplicate Transfers | ✅ Handled (Idempotency Key) | HIGH | Implemented |
| Transaction Code Collision | ✅ Handled (Unique Index) | MEDIUM | Implemented |
| Daily Limit Concurrent Check | ⚠️ Potential Issue | HIGH | **TODO: Add Distributed Lock** |
| Customer Status & Transfer | ⚠️ Eventually Consistent | MEDIUM | Event-driven (acceptable) |
| Concurrent Transfer Updates | ❌ Not Protected | LOW | TODO: Add RowVersion |
| Event Publishing Failure | ❌ Not Transactional | MEDIUM | **TODO: Outbox Pattern** |
| Multiple Service Instances | ⚠️ Partial | HIGH | **TODO: Distributed Lock** |

## 10. Implementation Roadmap

### Phase 1: Critical (Immediate)
- [ ] **Distributed Lock** for daily limit check (Redis)
- [ ] **Database Transactions** for create transfer flow
- [ ] **Retry mechanism** for external service failures

### Phase 2: Important (Short-term)
- [ ] **Unit of Work Pattern** for atomic operations
- [ ] **Outbox Pattern** for reliable event publishing
- [ ] **Optimistic Concurrency** with RowVersion

### Phase 3: Enhancement (Long-term)
- [ ] **Saga Pattern** for distributed transactions
- [ ] **Event Sourcing** for audit trail
- [ ] **CQRS** for read/write separation

## 11. Testing Race Conditions

### Concurrent Transfer Creation Test

```csharp
[Fact]
public async Task ConcurrentTransfers_ShouldRespectDailyLimit()
{
    // Arrange
    var senderId = Guid.NewGuid();
    var tasks = new List<Task<CreateTransferResponse>>();
    
    // Act: 3 concurrent requests of 4000 TRY each (total 12,000 > 10,000 limit)
    for (int i = 0; i < 3; i++)
    {
        tasks.Add(Task.Run(() => _transferService.CreateTransferAsync(new CreateTransferRequest
        {
            SenderId = senderId,
            Amount = 4000,
            Currency = Currency.TRY
        })));
    }
    
    var results = await Task.WhenAll(tasks);
    
    // Assert: Only 2 should succeed (8000 TRY), 1 should fail
    var successful = results.Count(r => r != null);
    Assert.Equal(2, successful);
}
```

### Idempotency Test

```csharp
[Fact]
public async Task DuplicateRequests_WithSameIdempotencyKey_ShouldReturnSameResult()
{
    // Arrange
    var idempotencyKey = Guid.NewGuid().ToString();
    var request = new CreateTransferRequest { IdempotencyKey = idempotencyKey, ... };
    
    // Act
    var result1 = await _transferService.CreateTransferAsync(request);
    var result2 = await _transferService.CreateTransferAsync(request);
    
    // Assert
    Assert.Equal(result1.TransferId, result2.TransferId);
    
    // Verify only 1 transfer created
    var count = await _context.Transfers.CountAsync(t => t.IdempotencyKey == idempotencyKey);
    Assert.Equal(1, count);
}
```

## Best Practices

### 1. Always Use Idempotency Keys
```csharp
// ✅ Good
var idempotencyKey = Guid.NewGuid().ToString();
await transferService.CreateAsync(new CreateTransferRequest 
{ 
    IdempotencyKey = idempotencyKey,
    ...
});

// ❌ Bad
await transferService.CreateAsync(new CreateTransferRequest { ... });
```

### 2. Wrap Critical Operations in Transactions
```csharp
// ✅ Good
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await DoOperation1();
    await DoOperation2();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 3. Use Distributed Locks for Shared Resources
```csharp
// ✅ Good
var lockKey = $"customer:{customerId}:daily-limit";
if (await _distributedLock.AcquireAsync(lockKey, TimeSpan.FromSeconds(10)))
{
    try
    {
        await CheckAndCreateTransfer();
    }
    finally
    {
        await _distributedLock.ReleaseAsync(lockKey);
    }
}
```

### 4. Handle Concurrency Exceptions
```csharp
// ✅ Good
int maxRetries = 3;
for (int i = 0; i < maxRetries; i++)
{
    try
    {
        await UpdateEntity();
        break;
    }
    catch (DbUpdateConcurrencyException)
    {
        if (i == maxRetries - 1) throw;
        await Task.Delay(100 * (i + 1));  // Exponential backoff
    }
}
```

### 5. Monitor and Alert
```csharp
_logger.LogWarning(
    "Concurrent modification detected for Transfer {TransferId}. Retry attempt {Attempt}",
    transferId,
    attemptNumber);

_metrics.Increment("concurrency_conflicts", tags: new[] { "entity:transfer" });
```

---

## Conclusion

Race condition'lar distributed system'lerde kaçınılmazdır. MoneyBee'de:

✅ **Implemented:**
- Idempotency for transfer creation
- Unique constraints for codes
- Event-driven architecture

⚠️ **Needs Attention:**
- Daily limit concurrent checks → **Redis distributed lock**
- Event publishing reliability → **Outbox pattern**
- Concurrent updates → **Optimistic concurrency**

🚀 **Recommended Next Steps:**
1. Implement Redis distributed lock for daily limit
2. Add Unit of Work pattern
3. Consider Outbox pattern for production
4. Add comprehensive concurrency tests
5. Monitor and measure actual conflict rates
