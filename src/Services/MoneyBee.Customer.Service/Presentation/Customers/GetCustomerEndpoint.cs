using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers.Queries.GetCustomerById;
using MoneyBee.Web.Common.Extensions;

namespace MoneyBee.Customer.Service.Presentation.Customers;

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
            .Produces<ApiResponse<GetCustomerByIdResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<GetCustomerByIdResponse>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetCustomerByIdHandler handler)
    {
        var result = await handler.HandleAsync(id);
        
        if (!result.IsSuccess)
            return result.ToHttpResult();
        
        return Results.Ok(ApiResponse<GetCustomerByIdResponse>.SuccessResponse(result.Value!));
    }
}
