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
    /// Gets all API keys in the system
    /// </summary>
    /// <returns>Collection of API key DTOs</returns>
    Task<IEnumerable<ApiKeyDto>> GetAllApiKeysAsync();
    
    /// <summary>
    /// Gets an API key by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the API key</param>
    /// <returns>Result containing the API key DTO if found</returns>
    Task<Result<ApiKeyDto>> GetApiKeyByIdAsync(Guid id);
    
    /// <summary>
    /// Updates an existing API key
    /// </summary>
    /// <param name="id">The unique identifier of the API key</param>
    /// <param name="request">The update request containing new values</param>
    /// <returns>Result containing the updated API key DTO</returns>
    Task<Result<ApiKeyDto>> UpdateApiKeyAsync(Guid id, UpdateApiKeyRequest request);
    
    /// <summary>
    /// Deletes an API key from the system
    /// </summary>
    /// <param name="id">The unique identifier of the API key to delete</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> DeleteApiKeyAsync(Guid id);
    Task<bool> ValidateApiKeyAsync(string apiKey);
    Task UpdateLastUsedAsync(string apiKey);
}
