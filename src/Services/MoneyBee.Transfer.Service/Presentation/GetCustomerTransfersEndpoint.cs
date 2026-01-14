using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.Transfers.Queries.GetCustomerTransfers;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;

namespace MoneyBee.Transfer.Service.Presentation;

/// <summary>
/// Get customer transfers endpoint
/// </summary>
public static class GetCustomerTransfersEndpoint
{
    public static RouteGroupBuilder MapGetCustomerTransfers(this RouteGroupBuilder group)
    {
        group.MapGet("/customer/{customerId:guid}", HandleAsync)
            .WithName("GetCustomerTransfers")
            .WithSummary("Get customer transfers")
            .Produces<ApiResponse<List<TransferDto>>>(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid customerId,
        GetCustomerTransfersHandler handler)
    {
        var dtos = await handler.HandleAsync(customerId);
        return Results.Ok(ApiResponse<List<TransferDto>>.SuccessResponse(dtos.ToList()));
    }
}
