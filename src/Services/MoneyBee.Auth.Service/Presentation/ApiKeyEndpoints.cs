namespace MoneyBee.Auth.Service.Presentation;

/// <summary>
/// API Key endpoints registration
/// </summary>
public static class ApiKeyEndpoints
{
    /// <summary>
    /// Maps all API Key endpoints to the route builder
    /// </summary>
    public static RouteGroupBuilder MapApiKeyEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth/keys")
            .WithTags("API Keys")
            .WithOpenApi();

        group.MapCreateApiKey();
        
        // Internal validation endpoint (not in group path)
        routes.MapValidateApiKeyForInternal();

        return group;
    }
}
