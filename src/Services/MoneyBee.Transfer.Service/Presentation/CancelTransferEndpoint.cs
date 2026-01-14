using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.Transfers;
using MoneyBee.Transfer.Service.Application.Transfers;
using MoneyBee.Web.Common.Extensions;

namespace MoneyBee.Transfer.Service.Presentation;

/// <summary>
/// Cancel transfer endpoint
/// </summary>
public static class CancelTransferEndpoint
{
    public static RouteGroupBuilder MapCancelTransfer(this RouteGroupBuilder group)
    {
        group.MapPost("/{code}/cancel", HandleAsync)
            .WithName("CancelTransfer")
            .WithSummary("Cancel a transfer")
            .Produces<ApiResponse<TransferDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<TransferDto>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<TransferDto>>(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string code,
        [FromBody] CancelTransferRequest request,
        ITransferService transferService,
        ILogger<Program> logger)
    {
        try
        {
            var result = await transferService.CancelTransferAsync(code, request);

            if (!result.IsSuccess)
                return result.ToHttpResult();

            return Results.Ok(ApiResponse<TransferDto>.SuccessResponse(result.Value!, "Transfer cancelled. Fee will be refunded."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling transfer");
            return Results.Problem(
                detail: "An error occurred while cancelling the transfer",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
