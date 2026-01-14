namespace MoneyBee.Auth.Service.Presentation.ApiKeys;

public static class ApiKeyEndpoints
{
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
