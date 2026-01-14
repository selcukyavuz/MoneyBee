using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers.Queries.GetCustomerByNationalId;
using MoneyBee.Web.Common.Extensions;

namespace MoneyBee.Customer.Service.Presentation.Customers;

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
            .Produces<ApiResponse<GetCustomerByNationalIdResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<GetCustomerByNationalIdResponse>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string nationalId,
        GetCustomerByNationalIdHandler handler)
    {
        var result = await handler.HandleAsync(nationalId);
        
        if (!result.IsSuccess)
            return result.ToHttpResult();
        
        return Results.Ok(ApiResponse<GetCustomerByNationalIdResponse>.SuccessResponse(result.Value!));
    }
}
