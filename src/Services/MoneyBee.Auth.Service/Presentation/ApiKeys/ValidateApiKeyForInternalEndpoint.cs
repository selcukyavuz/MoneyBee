using Microsoft.AspNetCore.Mvc;
using MoneyBee.Auth.Service.Application.ApiKeys.Queries.ValidateApiKey;

namespace MoneyBee.Auth.Service.Presentation.ApiKeys;

public static class ValidateApiKeyEndpoint
{
    public static void MapValidateApiKeyForInternal(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/v1/apikeys/validate", async (
            [FromBody] ValidateRequest request,
            ValidateApiKeyHandler handler) =>
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return Results.BadRequest(new { error = "API Key is required" });
            }

            var result = await handler.HandleAsync(request.ApiKey);
            
            return Results.Ok(new ValidateResponse 
            { 
                IsValid = result.IsSuccess && result.Value,
                Error = result.IsSuccess ? null : result.Error
            });
        })
        .WithName("ValidateApiKeyInternal")
        .WithDescription("Internal endpoint for API key validation by other services")
        .WithOpenApi();
    }

    public record ValidateRequest(string ApiKey);
    public record ValidateResponse
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
    }
}
