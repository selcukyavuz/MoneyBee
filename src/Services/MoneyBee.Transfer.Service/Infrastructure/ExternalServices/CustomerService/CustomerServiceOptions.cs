namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.CustomerService;

public class CustomerServiceOptions
{
    public required string BaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 10;
}
