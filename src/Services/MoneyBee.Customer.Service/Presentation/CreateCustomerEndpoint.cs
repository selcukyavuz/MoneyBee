using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers;
using MoneyBee.Customer.Service.Application.Customers;
using MoneyBee.Customer.Service.Extensions;

namespace MoneyBee.Customer.Service.Presentation;

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
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<CustomerDto>>(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateCustomerRequest request,
        ICustomerService customerService)
    {
        var result = await customerService.CreateCustomerAsync(request);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Created(
            $"/api/customers/{result.Value!.Id}",
            ApiResponse<CustomerDto>.SuccessResponse(result.Value, "Customer created successfully"));
    }
}
