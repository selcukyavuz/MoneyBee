using MoneyBee.Common.Enums;
using MoneyBee.Common.Results;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;

namespace MoneyBee.Transfer.Service.Application.Transfers.Services;

/// <summary>
/// Service interface for retrieving currency exchange rates
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Gets the exchange rate between two currencies
    /// </summary>
    /// <param name="fromCurrency">The source currency</param>
    /// <param name="toCurrency">The target currency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exchange rate result with rate and timestamp</returns>
    Task<Result<ExchangeRateResult>> GetExchangeRateAsync(Currency fromCurrency, Currency toCurrency, CancellationToken cancellationToken = default);
}
