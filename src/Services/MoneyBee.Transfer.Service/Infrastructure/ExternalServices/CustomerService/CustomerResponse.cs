using MoneyBee.Transfer.Service.Application.Transfers.Shared;

namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.CustomerService;

internal record CustomerResponse
{
    public CustomerInfo? Data { get; init; }
}
