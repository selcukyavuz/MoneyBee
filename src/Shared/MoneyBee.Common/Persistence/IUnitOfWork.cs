using Microsoft.EntityFrameworkCore;

namespace MoneyBee.Common.Persistence;

/// <summary>
/// Unit of Work pattern for managing transactions and domain events
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes and dispatches domain events atomically
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Begins a new database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
