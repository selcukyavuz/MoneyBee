using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.DTOs;
using MoneyBee.Customer.Service.Application.Interfaces;

namespace MoneyBee.Customer.Service.Endpoints;

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
        ICustomerService customerService)
    {
        var response = await customerService.VerifyCustomerAsync(nationalId);
        return Results.Ok(ApiResponse<CustomerVerificationResponse>.SuccessResponse(response));
    }
}
