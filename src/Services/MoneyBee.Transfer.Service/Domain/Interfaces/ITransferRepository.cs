using MoneyBee.Common.DDD;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;

namespace MoneyBee.Transfer.Service.Domain.Interfaces;

public interface ITransferRepository
{
    Task<TransferEntity?> GetByIdAsync(Guid id);
    Task<TransferEntity?> GetByTransactionCodeAsync(string transactionCode);
    Task<TransferEntity?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<IEnumerable<TransferEntity>> GetCustomerTransfersAsync(Guid customerId, int limit = 50);
    Task<decimal> GetDailyTotalAsync(Guid customerId, DateTime date);
    Task<IEnumerable<TransferEntity>> FindAsync(ISpecification<TransferEntity> specification);
    Task<TransferEntity> CreateAsync(TransferEntity transfer);
    Task<TransferEntity> UpdateAsync(TransferEntity transfer);
    Task<bool> TransactionCodeExistsAsync(string transactionCode);
}
