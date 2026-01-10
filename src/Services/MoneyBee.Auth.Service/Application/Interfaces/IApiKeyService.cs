using MoneyBee.Auth.Service.Application.DTOs;

namespace MoneyBee.Auth.Service.Application.Interfaces;

public interface IApiKeyService
{
    Task<CreateApiKeyResponse> CreateApiKeyAsync(CreateApiKeyRequest request);
    Task<IEnumerable<ApiKeyDto>> GetAllApiKeysAsync();
    Task<ApiKeyDto?> GetApiKeyByIdAsync(Guid id);
    Task<ApiKeyDto?> UpdateApiKeyAsync(Guid id, UpdateApiKeyRequest request);
    Task<bool> DeleteApiKeyAsync(Guid id);
    Task<bool> ValidateApiKeyAsync(string apiKey);
    Task UpdateLastUsedAsync(string apiKey);
}
