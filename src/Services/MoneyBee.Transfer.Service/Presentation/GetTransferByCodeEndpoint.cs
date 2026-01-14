using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.Transfers;
using MoneyBee.Transfer.Service.Application.Transfers;
using MoneyBee.Web.Common.Extensions;

namespace MoneyBee.Transfer.Service.Presentation;

/// <summary>
/// Get transfer by code endpoint
/// </summary>
public static class GetTransferByCodeEndpoint
{
    public static RouteGroupBuilder MapGetTransferByCode(this RouteGroupBuilder group)
    {
        group.MapGet("/{code}", HandleAsync)
            .WithName("GetTransferByCode")
            .WithSummary("Get transfer by transaction code")
            .Produces<ApiResponse<TransferDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<TransferDto>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string code,
        ITransferService transferService)
    {
        var result = await transferService.GetTransferByCodeAsync(code);
        return result.ToHttpResult();
    }
}
