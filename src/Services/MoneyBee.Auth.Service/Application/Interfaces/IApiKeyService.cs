using MoneyBee.Auth.Service.Application.DTOs;
using MoneyBee.Common.Results;

namespace MoneyBee.Auth.Service.Application.Interfaces;

/// <summary>
/// Service interface for managing API Keys
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Creates a new API Key
    /// </summary>
    /// <param name="request">The request containing API Key details</param>
    /// <returns>The created API key response with the generated key</returns>
    Task<CreateApiKeyResponse> CreateApiKeyAsync(CreateApiKeyRequest request);
    
    /// <summary>
    /// Validates an API key by checking if it exists and is active
    /// </summary>
    /// <param name="apiKey">The API key string to validate</param>
    /// <returns>True if the API key is valid and active; otherwise, false</returns>
    Task<bool> ValidateApiKeyAsync(string apiKey);
    
    /// <summary>
    /// Updates the last used timestamp for an API key
    /// </summary>
    /// <param name="apiKey">The API key string whose last used timestamp should be updated</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task UpdateLastUsedAsync(string apiKey);
}
