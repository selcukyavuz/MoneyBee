using Microsoft.AspNetCore.Mvc;
using MoneyBee.Auth.Service.Application.Interfaces;

namespace MoneyBee.Auth.Service.Endpoints;

public static class ValidateApiKeyEndpoint
{
    public static void MapValidateApiKeyForInternal(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/v1/apikeys/validate", async (
            [FromBody] ValidateRequest request,
            IApiKeyService apiKeyService) =>
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return Results.BadRequest(new { error = "API Key is required" });
            }

            var isValid = await apiKeyService.ValidateApiKeyAsync(request.ApiKey);
            
            return Results.Ok(new ValidateResponse { IsValid = isValid });
        })
        .WithName("ValidateApiKeyInternal")
        .WithDescription("Internal endpoint for API key validation by other services")
        .WithOpenApi();
    }

    public record ValidateRequest(string ApiKey);
    public record ValidateResponse
    {
        public bool IsValid { get; set; }
    }
}
