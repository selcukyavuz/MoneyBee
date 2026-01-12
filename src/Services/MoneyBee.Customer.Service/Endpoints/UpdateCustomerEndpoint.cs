using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.DTOs;
using MoneyBee.Customer.Service.Application.Interfaces;

namespace MoneyBee.Customer.Service.Endpoints;

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
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        ICustomerService customerService)
    {
        var result = await customerService.UpdateCustomerAsync(id, request);

        if (!result.IsSuccess)
            return Results.NotFound(ApiResponse<CustomerDto>.ErrorResponse(result.Error!));

        return Results.Ok(ApiResponse<CustomerDto>.SuccessResponse(result.Value!, "Customer updated successfully"));
    }
}
