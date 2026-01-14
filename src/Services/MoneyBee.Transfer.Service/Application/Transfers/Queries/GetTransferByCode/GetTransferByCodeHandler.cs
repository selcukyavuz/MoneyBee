using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Results;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;
using MoneyBee.Transfer.Service.Domain.Transfers;

namespace MoneyBee.Transfer.Service.Application.Transfers.Queries.GetTransferByCode;

/// <summary>
/// Handles retrieving a transfer by its transaction code
/// </summary>
public class GetTransferByCodeHandler(
    ITransferRepository repository) : IQueryHandler<string, Result<TransferDto>>
{
    public async Task<Result<TransferDto>> HandleAsync(string transactionCode, CancellationToken cancellationToken = default)
    {
        var transfer = await repository.GetByTransactionCodeAsync(transactionCode);
        
        if (transfer is null)
        {
            return Result<TransferDto>.Failure(TransferErrors.TransferNotFound);
        }
        
        return Result<TransferDto>.Success(TransferMapper.ToDto(transfer));
    }
}
