using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.DTOs;
using MoneyBee.Customer.Service.Application.Interfaces;

namespace MoneyBee.Customer.Service.Endpoints;

/// <summary>
/// Get all customers endpoint
/// </summary>
public static class GetCustomersEndpoint
{
    public static RouteGroupBuilder MapGetCustomers(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("GetCustomers")
            .WithSummary("Get all customers with pagination")
            .Produces<ApiResponse<IEnumerable<CustomerDto>>>(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        ICustomerService customerService)
    {
        var dtos = await customerService.GetAllCustomersAsync(pageNumber, pageSize);
        return Results.Ok(ApiResponse<IEnumerable<CustomerDto>>.SuccessResponse(dtos));
    }
}
