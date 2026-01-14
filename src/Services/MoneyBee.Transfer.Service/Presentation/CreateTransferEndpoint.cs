using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.Transfers.Commands.CreateTransfer;
using MoneyBee.Web.Common.Extensions;

namespace MoneyBee.Transfer.Service.Presentation;

/// <summary>
/// Create transfer endpoint
/// </summary>
public static class CreateTransferEndpoint
{
    public static RouteGroupBuilder MapCreateTransfer(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("CreateTransfer")
            .WithSummary("Create a new transfer (Money Sending)")
            .Produces<ApiResponse<CreateTransferResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<CreateTransferResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<CreateTransferResponse>>(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateTransferRequest request,
        CreateTransferHandler handler,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return result.ToHttpResult();

            var response = result.Value!;
            return Results.Created(
                $"/api/transfers/{response.TransactionCode}",
                ApiResponse<CreateTransferResponse>.SuccessResponse(response, response.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating transfer");
            return Results.Problem(
                detail: "An error occurred while processing the transfer",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
