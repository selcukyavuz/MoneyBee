using MoneyBee.Auth.Service.Application.DTOs;
using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Common.Models;

namespace MoneyBee.Auth.Service.Endpoints;

/// <summary>
/// Get all API Keys endpoint
/// </summary>
public static class GetAllApiKeysEndpoint
{
    public static RouteGroupBuilder MapGetAllApiKeys(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("GetAllApiKeys")
            .WithSummary("Get all API Keys (without actual keys)")
            .Produces<ApiResponse<IEnumerable<ApiKeyDto>>>(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> HandleAsync(IApiKeyService apiKeyService)
    {
        var keys = await apiKeyService.GetAllApiKeysAsync();
        return Results.Ok(ApiResponse<IEnumerable<ApiKeyDto>>.SuccessResponse(keys));
    }
}
