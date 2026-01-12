using Microsoft.AspNetCore.Mvc;
using MoneyBee.Auth.Service.Application.DTOs;
using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Common.Models;

namespace MoneyBee.Auth.Service.Endpoints;

/// <summary>
/// Create API Key endpoint
/// </summary>
public static class CreateApiKeyEndpoint
{
    public static RouteGroupBuilder MapCreateApiKey(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("CreateApiKey")
            .WithSummary("Create a new API Key")
            .Produces<ApiResponse<CreateApiKeyResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<CreateApiKeyResponse>>(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateApiKeyRequest request,
        IApiKeyService apiKeyService)
    {
        var response = await apiKeyService.CreateApiKeyAsync(request);

        return Results.Created(
            $"/api/auth/keys/{response.Id}",
            ApiResponse<CreateApiKeyResponse>.SuccessResponse(response, "API Key created successfully"));
    }
}
