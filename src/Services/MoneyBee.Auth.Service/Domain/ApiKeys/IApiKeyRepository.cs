using MoneyBee.Auth.Service.Domain.ApiKeys;

namespace MoneyBee.Auth.Service.Domain.ApiKeys;

/// <summary>
/// Repository interface for API Key data access operations
/// </summary>
public interface IApiKeyRepository
{
    /// <summary>
    /// Gets an API key by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <returns>The API key if found, null otherwise</returns>
    Task<ApiKey?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Gets an API key by its SHA256 hash
    /// </summary>
    /// <param name="keyHash">The hashed API key</param>
    /// <returns>The API key if found, null otherwise</returns>
    Task<ApiKey?> GetByKeyHashAsync(string keyHash);
    
    /// <summary>
    /// Gets all API keys in the system
    /// </summary>
    /// <returns>Collection of all API keys</returns>
    Task<IEnumerable<ApiKey>> GetAllAsync();
    
    /// <summary>
    /// Creates a new API key
    /// </summary>
    /// <param name="apiKey">The API key to create</param>
    /// <returns>The created API key</returns>
    Task<ApiKey> CreateAsync(ApiKey apiKey);
    
    /// <summary>
    /// Updates an existing API key
    /// </summary>
    /// <param name="apiKey">The API key with updated values</param>
    /// <returns>The updated API key</returns>
    Task<ApiKey> UpdateAsync(ApiKey apiKey);
    
    /// <summary>
    /// Deletes an API key by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the API key to delete</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteAsync(Guid id);
    
    /// <summary>
    /// Checks if an API key exists by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier to check</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id);
}
