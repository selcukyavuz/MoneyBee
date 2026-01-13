namespace MoneyBee.Auth.Service.Endpoints;

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

        return group;
    }
}
