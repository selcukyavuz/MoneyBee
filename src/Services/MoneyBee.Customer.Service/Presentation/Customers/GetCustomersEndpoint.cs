using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers;
using MoneyBee.Customer.Service.Application.Customers.Queries.GetAllCustomers;

namespace MoneyBee.Customer.Service.Presentation.Customers;

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
        GetAllCustomersHandler handler)
    {
        var dtos = await handler.HandleAsync(new GetAllCustomersRequest(pageNumber, pageSize));
        return Results.Ok(ApiResponse<IEnumerable<CustomerDto>>.SuccessResponse(dtos));
    }
}
