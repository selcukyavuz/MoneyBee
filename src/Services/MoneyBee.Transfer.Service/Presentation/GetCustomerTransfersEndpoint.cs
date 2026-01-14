using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.Transfers;
using MoneyBee.Transfer.Service.Application.Transfers;

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
        ITransferService transferService)
    {
        var dtos = await transferService.GetCustomerTransfersAsync(customerId);
        return Results.Ok(ApiResponse<List<TransferDto>>.SuccessResponse(dtos.ToList()));
    }
}
