using MoneyBee.Common.Enums;
using MoneyBee.Common.Exceptions;
using Polly;
using Polly.CircuitBreaker;

namespace MoneyBee.Transfer.Service.Services;

public interface IExchangeRateService
{
    Task<ExchangeRateResult> GetExchangeRateAsync(Currency fromCurrency, Currency toCurrency);
}

public class ExchangeRateResult
{
    public decimal Rate { get; set; }
    public Currency FromCurrency { get; set; }
    public Currency ToCurrency { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _circuitBreakerPipeline;

    public ExchangeRateService(
        IHttpClientFactory httpClientFactory,
        ILogger<ExchangeRateService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("ExchangeRateService");
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:ExchangeRateService"] ?? "http://exchange-rate-service");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _logger = logger;

        _circuitBreakerPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => !r.IsSuccessStatusCode)
            })
            .Build();
    }

    public async Task<ExchangeRateResult> GetExchangeRateAsync(Currency fromCurrency, Currency toCurrency)
    {
        // If both currencies are the same, return 1:1 rate
        if (fromCurrency == toCurrency)
        {
            return new ExchangeRateResult
            {
                Rate = 1.0m,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                Timestamp = DateTime.UtcNow
            };
        }

        try
        {
            var response = await _circuitBreakerPipeline.ExecuteAsync(async ct =>
            {
                _logger.LogInformation("Calling Exchange Rate Service: {From} -> {To}", fromCurrency, toCurrency);

                return await _httpClient.GetAsync($"/api/rates/{fromCurrency}/{toCurrency}", ct);
            }, CancellationToken.None);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Exchange Rate Service returned error: {StatusCode}", response.StatusCode);
                throw new ExternalServiceException("ExchangeRateService", "Failed to get exchange rate");
            }

            var result = await response.Content.ReadFromJsonAsync<ExchangeRateResult>();
            
            if (result == null || result.Rate <= 0)
            {
                throw new ExternalServiceException("ExchangeRateService", "Invalid exchange rate received");
            }

            _logger.LogInformation("Exchange rate {From}/{To}: {Rate}", fromCurrency, toCurrency, result.Rate);

            return result;
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("Exchange Rate Service circuit breaker is open");
            throw new ExternalServiceException("ExchangeRateService", "Service temporarily unavailable");
        }
        catch (Exception ex) when (ex is not ExternalServiceException)
        {
            _logger.LogError(ex, "Error calling Exchange Rate Service");
            throw new ExternalServiceException("ExchangeRateService", "Error getting exchange rate", ex);
        }
    }
}
