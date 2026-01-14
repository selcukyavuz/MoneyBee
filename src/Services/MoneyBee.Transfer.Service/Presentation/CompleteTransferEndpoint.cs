using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.Transfers.Commands.CompleteTransfer;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;
using MoneyBee.Web.Common.Extensions;

namespace MoneyBee.Transfer.Service.Presentation;

/// <summary>
/// Complete transfer endpoint
/// </summary>
public static class CompleteTransferEndpoint
{
    public static RouteGroupBuilder MapCompleteTransfer(this RouteGroupBuilder group)
    {
        group.MapPost("/{code}/complete", HandleAsync)
            .WithName("CompleteTransfer")
            .WithSummary("Complete a transfer (Money Receiving)")
            .Produces<ApiResponse<TransferDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<TransferDto>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<TransferDto>>(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string code,
        CompleteTransferHandler handler,
        ILogger<Program> logger)
    {
        try
        {
            var result = await handler.HandleAsync(code);

            if (!result.IsSuccess)
                return result.ToHttpResult();

            return Results.Ok(ApiResponse<TransferDto>.SuccessResponse(result.Value!, "Transfer completed successfully"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing transfer");
            return Results.Problem(
                detail: "An error occurred while completing the transfer",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
