using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers;
using MoneyBee.Customer.Service.Application.Customers;
using MoneyBee.Customer.Service.Extensions;

namespace MoneyBee.Customer.Service.Presentation;

/// <summary>
/// Update customer status endpoint
/// </summary>
public static class UpdateCustomerStatusEndpoint
{
    public static RouteGroupBuilder MapUpdateCustomerStatus(this RouteGroupBuilder group)
    {
        group.MapPatch("/{id:guid}/status", HandleAsync)
            .WithName("UpdateCustomerStatus")
            .WithSummary("Update customer status (Active/Passive/Blocked)")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromBody] UpdateCustomerStatusRequest request,
        ICustomerService customerService)
    {
        var result = await customerService.UpdateCustomerStatusAsync(id, request);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Ok(ApiResponse<object>.SuccessResponse(new { }, "Customer status updated successfully"));
    }
}
