using Microsoft.AspNetCore.Mvc;
using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Common.Models;

namespace MoneyBee.Auth.Service.Endpoints;

/// <summary>
/// Validate API Key endpoint
/// </summary>
public static class ValidateApiKeyEndpoint
{
    public static RouteGroupBuilder MapValidateApiKey(this RouteGroupBuilder group)
    {
        group.MapPost("/validate", HandleAsync)
            .WithName("ValidateApiKey")
            .WithSummary("Validate an API Key")
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] string apiKey,
        IApiKeyService apiKeyService)
    {
        var isValid = await apiKeyService.ValidateApiKeyAsync(apiKey);
        var message = isValid ? "Valid API Key" : "Invalid API Key";
        return Results.Ok(ApiResponse<bool>.SuccessResponse(isValid, message));
    }
}
