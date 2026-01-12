using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.DTOs;
using MoneyBee.Transfer.Service.Application.Interfaces;

namespace MoneyBee.Transfer.Service.Endpoints;

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
        ITransferService transferService,
        ILogger<Program> logger)
    {
        try
        {
            var result = await transferService.CreateTransferAsync(request);

            if (!result.IsSuccess)
                return Results.BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse(result.Error!));

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
