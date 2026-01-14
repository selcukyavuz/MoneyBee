using MoneyBee.Common.Abstractions;
using MoneyBee.Auth.Service.Application.ApiKeys.Queries.ValidateApiKey;

namespace MoneyBee.Auth.Service.Infrastructure.ApiKeys;

/// <summary>
/// Direct database API key validator for Auth Service
/// </summary>
public class DirectApiKeyValidator(ValidateApiKeyHandler handler) : IApiKeyValidator
{
    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        var result = await handler.HandleAsync(apiKey);
        return result.IsSuccess && result.Value;
    }
}
