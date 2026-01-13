namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.FraudDetectionService;

public class FraudDetectionServiceOptions
{
    public required string BaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 10;
}
