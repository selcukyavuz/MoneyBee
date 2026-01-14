using MoneyBee.Common.Abstractions;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;
using MoneyBee.Transfer.Service.Domain.Transfers;

namespace MoneyBee.Transfer.Service.Application.Transfers.Queries.GetCustomerTransfers;

/// <summary>
/// Handles retrieving all transfers for a specific customer
/// </summary>
public class GetCustomerTransfersHandler(
    ITransferRepository repository) : IQueryHandler<Guid, IEnumerable<TransferDto>>
{
    public async Task<IEnumerable<TransferDto>> HandleAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var transfers = await repository.GetCustomerTransfersAsync(customerId);
        return transfers.Select(TransferMapper.ToDto);
    }
}
