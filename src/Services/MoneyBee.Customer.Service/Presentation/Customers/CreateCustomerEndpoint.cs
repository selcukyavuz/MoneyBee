using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers.Commands.CreateCustomer;
using MoneyBee.Web.Common.Extensions;

namespace MoneyBee.Customer.Service.Presentation.Customers;

/// <summary>
/// Create customer endpoint
/// </summary>
public static class CreateCustomerEndpoint
{
    public static RouteGroupBuilder MapCreateCustomer(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("CreateCustomer")
            .WithSummary("Create a new customer with KYC verification")
            .Produces<ApiResponse<CreateCustomerResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<CreateCustomerResponse>>(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateCustomerRequest request,
        CreateCustomerHandler handler)
    {
        var result = await handler.HandleAsync(request);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Created(
            $"/api/customers/{result.Value!.Id}",
            ApiResponse<CreateCustomerResponse>.SuccessResponse(result.Value, "Customer created successfully"));
    }
}
