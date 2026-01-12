using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.DTOs;
using MoneyBee.Customer.Service.Application.Interfaces;

namespace MoneyBee.Customer.Service.Endpoints;

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

        if (!result.IsSuccess)
            return Results.NotFound(ApiResponse<CustomerDto>.ErrorResponse(result.Error!));

        return Results.Ok(ApiResponse<CustomerDto>.SuccessResponse(result.Value!));
    }
}
