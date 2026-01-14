using MoneyBee.Common.Enums;
using MoneyBee.Common.Results;
using MoneyBee.Transfer.Service.Application.Transfers.Services;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;
using MoneyBee.Transfer.Service.Infrastructure.Constants;

namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.ExchangeRateService;

public class ExchangeRateService(
    HttpClient httpClient,
    ILogger<ExchangeRateService> logger) : IExchangeRateService
{

    public async Task<Result<ExchangeRateResult>> GetExchangeRateAsync(Currency fromCurrency, Currency toCurrency, CancellationToken cancellationToken = default)
    {
        // If both currencies are the same, return 1:1 rate
        if (fromCurrency == toCurrency)
        {
            return Result<ExchangeRateResult>.Success(new ExchangeRateResult
            {
                Rate = 1.0m,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                Timestamp = DateTime.UtcNow
            });
        }

        try
        {
            logger.LogInformation("Fetching exchange rate from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);

            var response = await httpClient.GetAsync($"{ExternalApiEndpoints.ExchangeRate.GetRate}?from={fromCurrency}&to={toCurrency}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Exchange Rate Service returned error: {StatusCode}", response.StatusCode);
                return Result<ExchangeRateResult>.Failure(TransferErrors.ExchangeRateFailed);
            }

            var result = await response.Content.ReadFromJsonAsync<ExchangeRateResult>();
            
            if (result == null || result.Rate <= 0)
            {
                return Result<ExchangeRateResult>.Failure(TransferErrors.ExchangeRateInvalid);
            }

            logger.LogInformation("Exchange rate {From}/{To}: {Rate}", fromCurrency, toCurrency, result.Rate);

            return Result<ExchangeRateResult>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Exchange Rate Service");
            return Result<ExchangeRateResult>.Failure(TransferErrors.ExchangeRateError);
        }
    }
}
