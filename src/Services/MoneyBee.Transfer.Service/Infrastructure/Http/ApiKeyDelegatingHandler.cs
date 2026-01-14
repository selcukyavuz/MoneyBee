using MoneyBee.Common.Constants;

namespace MoneyBee.Transfer.Service.Infrastructure.Http;

public class ApiKeyDelegatingHandler(
    IHttpContextAccessor httpContextAccessor,
    ILogger<ApiKeyDelegatingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get X-Api-Key from incoming request
        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext?.Request.Headers.TryGetValue(HttpHeaders.ApiKey, out var apiKeyValues) == true)
        {
            var apiKey = apiKeyValues.FirstOrDefault();
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                // Forward X-Api-Key to outgoing request
                request.Headers.TryAddWithoutValidation(HttpHeaders.ApiKey, apiKey);
                logger.LogDebug("Forwarding X-Api-Key to {RequestUri}", request.RequestUri);
            }
            else
            {
                logger.LogWarning("X-Api-Key header is empty in incoming request");
            }
        }
        else
        {
            logger.LogWarning("X-Api-Key header not found in incoming request");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
