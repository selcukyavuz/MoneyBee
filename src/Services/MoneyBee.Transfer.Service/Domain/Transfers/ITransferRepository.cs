using TransferEntity = MoneyBee.Transfer.Service.Domain.Transfers.Transfer;

namespace MoneyBee.Transfer.Service.Domain.Transfers;

/// <summary>
/// Repository interface for transfer data access operations
/// </summary>
public interface ITransferRepository
{
    /// <summary>
    /// Gets a transfer by its unique identifier
    /// </summary>
    /// <param name="id">The transfer's unique identifier</param>
    /// <returns>The transfer entity if found, null otherwise</returns>
    Task<TransferEntity?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Gets a transfer by its transaction code
    /// </summary>
    /// <param name="transactionCode">The 8-digit transaction code</param>
    /// <returns>The transfer entity if found, null otherwise</returns>
    Task<TransferEntity?> GetByTransactionCodeAsync(string transactionCode);
    
    /// <summary>
    /// Gets a transfer by its idempotency key for duplicate detection
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key</param>
    /// <returns>The transfer entity if found, null otherwise</returns>
    Task<TransferEntity?> GetByIdempotencyKeyAsync(string idempotencyKey);

    /// <summary>
    /// Gets all pending transfers for a specific customer (as sender or receiver)
    /// </summary>
    /// <param name="customerId">The customer's unique identifier</param>
    /// <returns>List of pending transfers</returns>
    Task<IEnumerable<TransferEntity>> GetPendingByCustomerIdAsync(Guid customerId);
    
    /// <summary>
    /// Gets all transfers for a specific customer
    /// </summary>
    /// <param name="customerId">The customer's unique identifier</param>
    /// <param name="limit">Maximum number of transfers to return (default: 50)</param>
    /// <returns>Collection of transfer entities</returns>
    Task<IEnumerable<TransferEntity>> GetCustomerTransfersAsync(Guid customerId, int limit = 50);
    
    /// <summary>
    /// Gets the total transfer amount for a customer on a specific date
    /// </summary>
    /// <param name="customerId">The customer's unique identifier</param>
    /// <param name="date">The date to check</param>
    /// <returns>Total transfer amount in TRY</returns>
    Task<decimal> GetDailyTotalAsync(Guid customerId, DateTime date);
    
    /// <summary>
    /// Creates a new transfer
    /// </summary>
    /// <param name="transfer">The transfer entity to create</param>
    /// <returns>The created transfer entity</returns>
    Task<TransferEntity> CreateAsync(TransferEntity transfer);
    
    /// <summary>
    /// Updates an existing transfer
    /// </summary>
    /// <param name="transfer">The transfer entity with updated values</param>
    /// <returns>The updated transfer entity</returns>
    Task<TransferEntity> UpdateAsync(TransferEntity transfer);
    
    /// <summary>
    /// Checks if a transaction code already exists
    /// </summary>
    /// <param name="transactionCode">The transaction code to check</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> TransactionCodeExistsAsync(string transactionCode);
}
