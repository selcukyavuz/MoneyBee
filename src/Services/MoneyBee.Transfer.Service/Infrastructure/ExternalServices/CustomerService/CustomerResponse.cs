namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.CustomerService;

internal record CustomerResponse
{
    public CustomerInfo? Data { get; init; }
}
