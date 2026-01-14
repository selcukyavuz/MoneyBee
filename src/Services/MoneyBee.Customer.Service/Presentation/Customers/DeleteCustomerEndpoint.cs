using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers.Commands.DeleteCustomer;

namespace MoneyBee.Customer.Service.Presentation.Customers;

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
        DeleteCustomerHandler handler)
    {
        var success = await handler.HandleAsync(id);

        if (!success)
            return Results.NotFound(ApiResponse<object>.ErrorResponse("Customer not found"));

        return Results.NoContent();
    }
}
