using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Common.Models;

namespace MoneyBee.Auth.Service.Endpoints;

/// <summary>
/// Delete API Key endpoint
/// </summary>
public static class DeleteApiKeyEndpoint
{
    public static RouteGroupBuilder MapDeleteApiKey(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", HandleAsync)
            .WithName("DeleteApiKey")
            .WithSummary("Delete API Key")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IApiKeyService apiKeyService)
    {
        var result = await apiKeyService.DeleteApiKeyAsync(id);

        if (!result.IsSuccess)
            return Results.NotFound(ApiResponse<object>.ErrorResponse(result.Error!));

        return Results.NoContent();
    }
}
