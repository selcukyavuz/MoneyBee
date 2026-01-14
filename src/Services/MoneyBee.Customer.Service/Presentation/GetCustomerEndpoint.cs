using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers;
using MoneyBee.Customer.Service.Application.Customers;
using MoneyBee.Web.Common.Extensions;

namespace MoneyBee.Customer.Service.Presentation;

/// <summary>
/// Get customer by ID endpoint
/// </summary>
public static class GetCustomerEndpoint
{
    public static RouteGroupBuilder MapGetCustomer(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetCustomer")
            .WithSummary("Get customer by ID")
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ICustomerService customerService)
    {
        var result = await customerService.GetCustomerByIdAsync(id);
        return result.ToHttpResult();
    }
}
