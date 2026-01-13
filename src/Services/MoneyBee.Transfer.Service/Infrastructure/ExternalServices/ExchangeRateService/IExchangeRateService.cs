using MoneyBee.Common.Enums;
using MoneyBee.Common.Results;

namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.ExchangeRateService;

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
    /// <returns>Exchange rate result with rate and timestamp</returns>
    Task<Result<ExchangeRateResult>> GetExchangeRateAsync(Currency fromCurrency, Currency toCurrency);
}
