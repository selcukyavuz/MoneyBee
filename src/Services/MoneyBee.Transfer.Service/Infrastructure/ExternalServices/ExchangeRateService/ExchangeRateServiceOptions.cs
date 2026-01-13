namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.ExchangeRateService;

public class ExchangeRateServiceOptions
{
    public required string BaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 10;
}
