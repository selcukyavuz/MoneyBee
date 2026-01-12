using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.Enums;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;
using MoneyBee.Transfer.Service.Domain.Interfaces;
using MoneyBee.Transfer.Service.Infrastructure.Data;

namespace MoneyBee.Transfer.Service.Infrastructure.Repositories;

public class TransferRepository : ITransferRepository
{
    private readonly TransferDbContext _context;

    public TransferRepository(TransferDbContext context)
    {
        _context = context;
    }

    public async Task<TransferEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Transfers.FindAsync(id);
    }

    public async Task<TransferEntity?> GetByTransactionCodeAsync(string transactionCode)
    {
        return await _context.Transfers
            .FirstOrDefaultAsync(t => t.TransactionCode == transactionCode);
    }

    public async Task<TransferEntity?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        return await _context.Transfers
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
    }

    public async Task<IEnumerable<TransferEntity>> GetCustomerTransfersAsync(Guid customerId, int limit = 50)
    {
        return await _context.Transfers
            .Where(t => t.SenderId == customerId || t.ReceiverId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<decimal> GetDailyTotalAsync(Guid customerId, DateTime date)
    {
        var startOfDay = date.Date;
        return await _context.Transfers
            .Where(t => t.SenderId == customerId &&
                       t.CreatedAt >= startOfDay &&
                       (t.Status == TransferStatus.Pending || t.Status == TransferStatus.Completed))
            .SumAsync(t => t.AmountInTRY);
    }

    public async Task<TransferEntity> CreateAsync(TransferEntity transfer)
    {
        _context.Transfers.Add(transfer);
        await _context.SaveChangesAsync();
        return transfer;
    }

    public async Task<TransferEntity> UpdateAsync(TransferEntity transfer)
    {
        _context.Transfers.Update(transfer);
        await _context.SaveChangesAsync();
        return transfer;
    }

    public async Task<bool> TransactionCodeExistsAsync(string transactionCode)
    {
        return await _context.Transfers.AnyAsync(t => t.TransactionCode == transactionCode);
    }
}
