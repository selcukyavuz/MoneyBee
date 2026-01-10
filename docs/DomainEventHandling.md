# Domain Event Handling Architecture

## Overview

Domain Event'ler, domain model içinde gerçekleşen önemli değişiklikleri temsil eder ve sistemimizdeki reaktif davranışları tetiklemek için kullanılır.

## Architecture Components

### 1. Domain Events (MoneyBee.Common.DDD)

**Base Class:**
```csharp
public abstract class DomainEvent
{
    public Guid EventId { get; protected set; }
    public DateTime OccurredOn { get; protected set; }
}
```

**Concrete Events:**
- `CustomerCreatedDomainEvent` - Yeni müşteri oluşturulduğunda
- `CustomerDeletedDomainEvent` - Müşteri silindiğinde
- `CustomerStatusChangedDomainEvent` - Müşteri durumu değiştiğinde
- `TransferCreatedDomainEvent` - Transfer oluşturulduğunda
- `TransferCompletedDomainEvent` - Transfer tamamlandığında
- `TransferCancelledDomainEvent` - Transfer iptal edildiğinde

### 2. Aggregate Roots

Aggregate root'lar domain event'leri toplar ve yönetir:

```csharp
public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = new();
    
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

**Örnek Kullanım:**
```csharp
public class Customer : AggregateRoot
{
    public static Customer Create(...)
    {
        var customer = new Customer { ... };
        
        // Domain event ekle
        customer.AddDomainEvent(new CustomerCreatedDomainEvent
        {
            CustomerId = customer.Id,
            NationalId = nationalId,
            FirstName = firstName,
            LastName = lastName,
            Email = email
        });
        
        return customer;
    }
    
    public void UpdateStatus(CustomerStatus newStatus)
    {
        var oldStatus = Status;
        Status = newStatus;
        
        // Durum değişikliği event'i
        AddDomainEvent(new CustomerStatusChangedDomainEvent
        {
            CustomerId = Id,
            OldStatus = oldStatus,
            NewStatus = newStatus
        });
    }
}
```

### 3. Domain Event Handlers

Her domain event için bir veya daha fazla handler oluşturulabilir:

```csharp
public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : DomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
```

**Örnek Handler:**
```csharp
public class CustomerCreatedDomainEventHandler : IDomainEventHandler<CustomerCreatedDomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger _logger;
    
    public async Task HandleAsync(CustomerCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CustomerCreatedDomainEvent for {CustomerId}", domainEvent.CustomerId);
        
        // 1. Integration event'e çevir ve RabbitMQ'ya gönder
        var integrationEvent = new CustomerCreatedEvent
        {
            CustomerId = domainEvent.CustomerId,
            NationalId = domainEvent.NationalId,
            FirstName = domainEvent.FirstName,
            LastName = domainEvent.LastName,
            Email = domainEvent.Email,
            Timestamp = domainEvent.OccurredOn,
            CorrelationId = domainEvent.EventId.ToString()
        };
        
        await _eventPublisher.PublishAsync(integrationEvent);
        
        // 2. Domain-specific logic
        // - Welcome email gönder
        // - Audit log oluştur
        // - Analytics güncelle
        // - Onboarding workflow başlat
    }
}
```

### 4. Domain Event Dispatcher

Event'leri ilgili handler'lara yönlendirir:

```csharp
public interface IDomainEventDispatcher
{
    Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
```

**Implementation:**
```csharp
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    
    public async Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        
        // İlgili tüm handler'ları bul
        var handlers = _serviceProvider.GetServices(handlerType);
        
        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod("HandleAsync");
            var task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
            await task;
        }
    }
}
```

## Event Flow

```
┌─────────────────┐
│  API Request    │
└────────┬────────┘
         │
         v
┌─────────────────┐
│ Application     │
│ Service         │
└────────┬────────┘
         │
         v
┌─────────────────┐      ┌──────────────────┐
│ Aggregate Root  │─────>│ Domain Event     │
│ (Customer)      │      │ (Created)        │
└────────┬────────┘      └──────────────────┘
         │                       │
         v                       │
┌─────────────────┐             │
│ Repository      │             │
│ SaveChanges     │             │
└────────┬────────┘             │
         │                       │
         v                       │
┌─────────────────┐             │
│ Event           │<────────────┘
│ Dispatcher      │
└────────┬────────┘
         │
         v
┌─────────────────────────────────────┐
│     Domain Event Handlers           │
├─────────────────────────────────────┤
│ 1. CustomerCreatedDomainEventHandler│
│    - Publish to RabbitMQ            │
│    - Send welcome email             │
│    - Create audit log               │
│    - Update analytics               │
│                                     │
│ 2. AuditLogHandler                  │
│    - Log to audit database          │
│                                     │
│ 3. AnalyticsHandler                 │
│    - Update metrics                 │
└─────────────────────────────────────┘
```

## Application Service Usage

```csharp
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    
    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request)
    {
        // 1. Create aggregate
        var customer = CustomerEntity.Create(...);
        
        // 2. Validate with domain service
        _domainService.ValidateCustomerForCreation(customer);
        
        // 3. Save to database
        await _repository.CreateAsync(customer);
        
        // 4. Dispatch domain events
        await _domainEventDispatcher.DispatchAsync(customer.DomainEvents);
        
        // 5. Clear events
        customer.ClearDomainEvents();
        
        return MapToDto(customer);
    }
}
```

## Dependency Injection Setup

### Customer Service (Program.cs)

```csharp
// Domain Event Dispatcher
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

// Domain Event Handlers
builder.Services.AddScoped<IDomainEventHandler<CustomerCreatedDomainEvent>, 
    CustomerCreatedDomainEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<CustomerDeletedDomainEvent>, 
    CustomerDeletedDomainEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<CustomerStatusChangedDomainEvent>, 
    CustomerStatusChangedDomainEventHandler>();
```

### Transfer Service (Program.cs)

```csharp
// Domain Event Dispatcher
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

// Domain Event Handlers
builder.Services.AddScoped<IDomainEventHandler<TransferCreatedDomainEvent>, 
    TransferCreatedDomainEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<TransferCompletedDomainEvent>, 
    TransferCompletedDomainEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<TransferCancelledDomainEvent>, 
    TransferCancelledDomainEventHandler>();
```

## Domain Events vs Integration Events

### Domain Events (In-Process)
- **Scope:** Tek bir bounded context içinde
- **Purpose:** Domain logic'i tetiklemek
- **Transport:** In-memory (same process)
- **Examples:**
  - Audit logging
  - Cascade operations
  - Local side effects
  - Validation

### Integration Events (Inter-Process)
- **Scope:** Farklı microservice'ler arası
- **Purpose:** Service'ler arası iletişim
- **Transport:** Message bus (RabbitMQ)
- **Examples:**
  - CustomerCreatedEvent → Transfer Service
  - CustomerStatusChangedEvent → Transfer Service
  - TransferCompletedEvent → Notification Service

**Conversion Pattern:**
```csharp
public class CustomerCreatedDomainEventHandler : IDomainEventHandler<CustomerCreatedDomainEvent>
{
    public async Task HandleAsync(CustomerCreatedDomainEvent domainEvent, ...)
    {
        // Domain event'i integration event'e çevir
        var integrationEvent = new CustomerCreatedEvent
        {
            CustomerId = domainEvent.CustomerId,
            NationalId = domainEvent.NationalId,
            ...
        };
        
        // RabbitMQ'ya gönder
        await _eventPublisher.PublishAsync(integrationEvent);
    }
}
```

## Handler Examples

### 1. TransferCreatedDomainEventHandler

```csharp
public async Task HandleAsync(TransferCreatedDomainEvent domainEvent, ...)
{
    // 1. Send SMS to sender with transaction code
    await SendSmsToSenderAsync(domainEvent);
    
    // 2. Create audit log
    await CreateAuditLogAsync(domainEvent);
    
    // 3. Update analytics/metrics
    await UpdateMetricsAsync(domainEvent);
}
```

### 2. TransferCompletedDomainEventHandler

```csharp
public async Task HandleAsync(TransferCompletedDomainEvent domainEvent, ...)
{
    // 1. Send completion notification to both parties
    await SendCompletionNotificationsAsync(domainEvent);
    
    // 2. Update customer loyalty points
    var points = CalculateLoyaltyPoints(domainEvent.AmountInTRY);
    await UpdateLoyaltyPointsAsync(points);
    
    // 3. Audit completion
    await AuditCompletionAsync(domainEvent);
    
    // 4. Analytics - track completion time
    await TrackCompletionMetricsAsync(domainEvent);
}
```

### 3. TransferCancelledDomainEventHandler

```csharp
public async Task HandleAsync(TransferCancelledDomainEvent domainEvent, ...)
{
    // 1. Process fee refund
    await ProcessFeeRefundAsync(domainEvent);
    
    // 2. Send cancellation notifications
    await SendCancellationNotificationsAsync(domainEvent);
    
    // 3. Audit cancellation
    await AuditCancellationAsync(domainEvent);
    
    // 4. Update cancellation metrics
    await TrackCancellationMetricsAsync(domainEvent);
}
```

### 4. CustomerStatusChangedDomainEventHandler

```csharp
public async Task HandleAsync(CustomerStatusChangedDomainEvent domainEvent, ...)
{
    // Integration event'e çevir ve RabbitMQ'ya gönder
    var integrationEvent = new CustomerStatusChangedEvent { ... };
    await _eventPublisher.PublishAsync(integrationEvent);
    
    // Status-specific logic
    switch (domainEvent.NewStatus)
    {
        case CustomerStatus.Blocked:
            // Alert compliance team
            // Cancel pending transfers
            break;
            
        case CustomerStatus.Active when domainEvent.OldStatus == CustomerStatus.Blocked:
            // Send welcome back notification
            // Resume services
            break;
    }
}
```

## Benefits

### 1. **Separation of Concerns**
- Business logic aggregate'lerde
- Side effects handler'larda
- Clear responsibilities

### 2. **Testability**
```csharp
[Fact]
public async Task CustomerCreatedEvent_ShouldPublishToRabbitMQ()
{
    // Arrange
    var handler = new CustomerCreatedDomainEventHandler(mockPublisher, mockLogger);
    var domainEvent = new CustomerCreatedDomainEvent { ... };
    
    // Act
    await handler.HandleAsync(domainEvent);
    
    // Assert
    mockPublisher.Verify(x => x.PublishAsync(It.IsAny<CustomerCreatedEvent>()), Times.Once);
}
```

### 3. **Extensibility**
Yeni handler eklemek kolay - mevcut kodu değiştirmeden:
```csharp
// Yeni handler ekle
public class CustomerCreatedEmailHandler : IDomainEventHandler<CustomerCreatedDomainEvent>
{
    public async Task HandleAsync(CustomerCreatedDomainEvent domainEvent, ...)
    {
        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(domainEvent.Email);
    }
}

// Program.cs'e kaydet
builder.Services.AddScoped<IDomainEventHandler<CustomerCreatedDomainEvent>, 
    CustomerCreatedEmailHandler>();
```

### 4. **Eventual Consistency**
Domain event'ler eventual consistency sağlar:
- Customer Service: Müşteri oluştur
- Event handler: RabbitMQ'ya gönder
- Transfer Service: Event'i dinle ve kendi veritabanını güncelle

### 5. **Audit Trail**
Tüm domain değişiklikleri event olarak kaydedilir:
- Ne oldu? (Event type)
- Ne zaman oldu? (OccurredOn)
- Kim tarafından? (Event data)
- Neden oldu? (Context data)

## Error Handling

```csharp
public class DomainEventDispatcher : IDomainEventDispatcher
{
    public async Task DispatchAsync(DomainEvent domainEvent, ...)
    {
        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling domain event {EventType} with handler {HandlerType}",
                    eventType.Name, handler.GetType().Name);
                    
                // Strategy:
                // 1. Log error
                // 2. Notify monitoring system
                // 3. Re-throw or continue based on criticality
                throw;
            }
        }
    }
}
```

## Best Practices

### 1. **Event Naming**
- Use past tense: `CustomerCreated`, `TransferCompleted`
- Be specific: `CustomerStatusChanged` not `CustomerUpdated`
- Include context: `TransferCancelledDomainEvent`

### 2. **Event Data**
- Include all relevant data
- Don't include entire aggregate
- Use immutable properties

### 3. **Handler Responsibilities**
- Keep handlers focused
- One handler per concern
- Async operations preferred

### 4. **Transaction Boundaries**
```csharp
// ✅ Correct: Events dispatched AFTER SaveChanges
await _repository.CreateAsync(customer);
await _domainEventDispatcher.DispatchAsync(customer.DomainEvents);

// ❌ Wrong: Events dispatched BEFORE SaveChanges
await _domainEventDispatcher.DispatchAsync(customer.DomainEvents);
await _repository.CreateAsync(customer);  // Might fail!
```

### 5. **Idempotency**
Handler'lar idempotent olmalı - aynı event birden fazla kez gelirse sorun çıkmamalı

## Future Enhancements

### 1. **Outbox Pattern**
Event'leri önce DB'ye yaz, sonra güvenilir şekilde publish et:
```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string EventData { get; set; }
    public DateTime OccurredOn { get; set; }
    public bool Published { get; set; }
}
```

### 2. **Event Store**
Tüm event'leri kalıcı olarak sakla (Event Sourcing):
```csharp
public interface IEventStore
{
    Task SaveAsync(DomainEvent domainEvent);
    Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId);
}
```

### 3. **Retry Mechanism**
Failed event handler'ları otomatik retry et:
```csharp
[Retry(maxAttempts: 3, delayMilliseconds: 1000)]
public class CustomerCreatedDomainEventHandler : IDomainEventHandler<...>
```

### 4. **Event Versioning**
Event şemaları zamanla değişebilir:
```csharp
public class CustomerCreatedDomainEvent_V2 : CustomerCreatedDomainEvent
{
    public string MiddleName { get; set; } // New field
}
```

## Summary

Domain Event Handling sistemi, MoneyBee'de:
- ✅ Business logic'i side effect'lerden ayırır
- ✅ Microservice'ler arası gevşek bağlantı sağlar
- ✅ Audit trail ve monitoring için foundation
- ✅ Test edilebilir ve genişletilebilir
- ✅ Eventual consistency destekler
- ✅ Domain-Driven Design prensiplerini takip eder
