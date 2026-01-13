using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Common.Services;

namespace MoneyBee.Auth.Service.Services;

/// <summary>
/// Direct database API key validator for Auth Service (no caching needed since we own the data)
/// </summary>
public class DirectApiKeyValidator : IApiKeyValidator
{
    private readonly IApiKeyService _apiKeyService;

    public DirectApiKeyValidator(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    /// <summary>
    /// Validates an API key directly against the database
    /// </summary>
    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        return await _apiKeyService.ValidateApiKeyAsync(apiKey);
    }
}
