using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MoneyBee.Common.DDD;
using System.Text.Json;

namespace MoneyBee.Common.Persistence;

/// <summary>
/// Base Unit of Work implementation for DbContext
/// Ensures domain events are dispatched after successful database transaction
/// </summary>
public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<UnitOfWork<TContext>> _logger;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(
        TContext context,
        IDomainEventDispatcher eventDispatcher,
        ILogger<UnitOfWork<TContext>> logger)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect all domain events before saving
        var aggregates = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        try
        {
            // Write domain events to outbox table atomically with business data
            if (domainEvents.Any())
            {
                var outboxMessages = domainEvents.Select(e => new OutboxMessage
                {
                    EventType = e.GetType().Name,
                    EventData = JsonSerializer.Serialize(e, e.GetType()),
                    OccurredOn = DateTime.UtcNow
                }).ToList();

                // Add outbox messages to DbContext (assumes DbSet<OutboxMessage> exists)
                await _context.Set<OutboxMessage>().AddRangeAsync(outboxMessages, cancellationToken);

                _logger.LogDebug(
                    "Added {EventCount} events to outbox for atomic persistence.",
                    domainEvents.Count);
            }

            // Save changes to database (business data + outbox messages in same transaction)
            var result = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Saved {Count} changes to database with {EventCount} events in outbox.",
                result, domainEvents.Count);

            // Clear events after successful save (they're now in outbox)
            foreach (var aggregate in aggregates)
            {
                aggregate.ClearDomainEvents();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error during SaveChanges. Changes rolled back. Events not dispatched.");
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            _logger.LogWarning("Transaction already in progress");
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("Database transaction started");
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Database transaction committed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            _logger.LogWarning("No transaction to rollback");
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Database transaction rolled back");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }
}
