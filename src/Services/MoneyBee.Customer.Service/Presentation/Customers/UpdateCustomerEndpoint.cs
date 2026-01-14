using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers.Commands.UpdateCustomer;
using MoneyBee.Web.Common.Extensions;

namespace MoneyBee.Customer.Service.Presentation.Customers;

/// <summary>
/// Update customer endpoint
/// </summary>
public static class UpdateCustomerEndpoint
{
    public static RouteGroupBuilder MapUpdateCustomer(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdateCustomer")
            .WithSummary("Update customer details")
            .Produces<ApiResponse<UpdateCustomerResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<UpdateCustomerResponse>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        UpdateCustomerHandler handler)
    {
        var result = await handler.HandleAsync(request with { Id = id });

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Ok(ApiResponse<UpdateCustomerResponse>.SuccessResponse(result.Value!, "Customer updated successfully"));
    }
}
