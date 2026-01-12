using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.DTOs;
using MoneyBee.Transfer.Service.Application.Interfaces;

namespace MoneyBee.Transfer.Service.Endpoints;

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

        if (!result.IsSuccess)
            return Results.NotFound(ApiResponse<TransferDto>.ErrorResponse(result.Error!));

        return Results.Ok(ApiResponse<TransferDto>.SuccessResponse(result.Value!));
    }
}
