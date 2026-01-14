using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.Customers.Queries.VerifyCustomer;
using MoneyBee.Customer.Service.Application.Customers;

namespace MoneyBee.Customer.Service.Presentation.Customers;

/// <summary>
/// Verify customer by National ID endpoint
/// </summary>
public static class VerifyCustomerEndpoint
{
    public static RouteGroupBuilder MapVerifyCustomer(this RouteGroupBuilder group)
    {
        group.MapGet("/verify/{nationalId}", HandleAsync)
            .WithName("VerifyCustomer")
            .WithSummary("Verify customer by National ID")
            .Produces<ApiResponse<CustomerVerificationResponse>>(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string nationalId,
        VerifyCustomerHandler handler)
    {
        var response = await handler.HandleAsync(nationalId);
        return Results.Ok(ApiResponse<CustomerVerificationResponse>.SuccessResponse(response));
    }
}
