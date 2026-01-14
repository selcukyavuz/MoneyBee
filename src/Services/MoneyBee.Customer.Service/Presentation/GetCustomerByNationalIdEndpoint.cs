using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers;
using MoneyBee.Customer.Service.Application.Customers;
using MoneyBee.Web.Common.Extensions;

namespace MoneyBee.Customer.Service.Presentation;

/// <summary>
/// Get customer by National ID endpoint
/// </summary>
public static class GetCustomerByNationalIdEndpoint
{
    public static RouteGroupBuilder MapGetCustomerByNationalId(this RouteGroupBuilder group)
    {
        group.MapGet("/by-national-id/{nationalId}", HandleAsync)
            .WithName("GetCustomerByNationalId")
            .WithSummary("Get customer details by National ID")
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string nationalId,
        ICustomerService customerService)
    {
        var result = await customerService.GetCustomerByNationalIdAsync(nationalId);
        return result.ToHttpResult();
    }
}
