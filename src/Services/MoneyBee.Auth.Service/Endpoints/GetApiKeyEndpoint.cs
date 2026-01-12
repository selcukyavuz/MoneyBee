using MoneyBee.Auth.Service.Application.DTOs;
using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Common.Models;

namespace MoneyBee.Auth.Service.Endpoints;

/// <summary>
/// Get API Key by ID endpoint
/// </summary>
public static class GetApiKeyEndpoint
{
    public static RouteGroupBuilder MapGetApiKey(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetApiKey")
            .WithSummary("Get specific API Key by ID")
            .Produces<ApiResponse<ApiKeyDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<ApiKeyDto>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IApiKeyService apiKeyService)
    {
        var result = await apiKeyService.GetApiKeyByIdAsync(id);

        if (!result.IsSuccess)
            return Results.NotFound(ApiResponse<ApiKeyDto>.ErrorResponse(result.Error!));

        return Results.Ok(ApiResponse<ApiKeyDto>.SuccessResponse(result.Value!));
    }
}
