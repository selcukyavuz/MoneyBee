using Microsoft.Extensions.Options;
using MoneyBee.Common.Abstractions;
using MoneyBee.Transfer.Service.Application.Transfers.Options;
using MoneyBee.Transfer.Service.Domain.Transfers;

namespace MoneyBee.Transfer.Service.Application.Transfers.Queries.CheckDailyLimit;

/// <summary>
/// Handles checking daily transfer limit for a customer
/// </summary>
public class CheckDailyLimitHandler(
    ITransferRepository repository,
    IOptions<TransferSettings> transferSettings) : IQueryHandler<Guid, DailyLimitCheckResponse>
{
    private readonly TransferSettings _transferSettings = transferSettings.Value;

    public async Task<DailyLimitCheckResponse> HandleAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var totalToday = await repository.GetDailyTotalAsync(customerId, DateTime.Today);

        return new DailyLimitCheckResponse
        {
            TotalTransfersToday = totalToday,
            DailyLimit = _transferSettings.DailyLimitTRY
        };
    }
}
