using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.Transfers.Queries.GetTransferByCode;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;
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
        GetTransferByCodeHandler handler)
    {
        var result = await handler.HandleAsync(code);
        return result.ToHttpResult();
    }
}
