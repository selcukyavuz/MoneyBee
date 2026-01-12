using Microsoft.AspNetCore.Mvc;
using MoneyBee.Auth.Service.Application.DTOs;
using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Common.Models;

namespace MoneyBee.Auth.Service.Endpoints;

/// <summary>
/// Update API Key endpoint
/// </summary>
public static class UpdateApiKeyEndpoint
{
    public static RouteGroupBuilder MapUpdateApiKey(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdateApiKey")
            .WithSummary("Update API Key details")
            .Produces<ApiResponse<ApiKeyDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<ApiKeyDto>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromBody] UpdateApiKeyRequest request,
        IApiKeyService apiKeyService)
    {
        var result = await apiKeyService.UpdateApiKeyAsync(id, request);

        if (!result.IsSuccess)
            return Results.NotFound(ApiResponse<ApiKeyDto>.ErrorResponse(result.Error!));

        return Results.Ok(ApiResponse<ApiKeyDto>.SuccessResponse(result.Value!, "API Key updated successfully"));
    }
}
