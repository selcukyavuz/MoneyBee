using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers;

namespace MoneyBee.Customer.Service.Presentation;

/// <summary>
/// Delete customer endpoint
/// </summary>
public static class DeleteCustomerEndpoint
{
    public static RouteGroupBuilder MapDeleteCustomer(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", HandleAsync)
            .WithName("DeleteCustomer")
            .WithSummary("Delete customer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ICustomerService customerService)
    {
        var success = await customerService.DeleteCustomerAsync(id);

        if (!success)
            return Results.NotFound(ApiResponse<object>.ErrorResponse("Customer not found"));

        return Results.NoContent();
    }
}
