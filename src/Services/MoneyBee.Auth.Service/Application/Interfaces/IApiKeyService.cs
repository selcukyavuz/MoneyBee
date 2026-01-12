using MoneyBee.Auth.Service.Application.DTOs;
using MoneyBee.Common.Results;

namespace MoneyBee.Auth.Service.Application.Interfaces;

public interface IApiKeyService
{
    Task<CreateApiKeyResponse> CreateApiKeyAsync(CreateApiKeyRequest request);
    Task<IEnumerable<ApiKeyDto>> GetAllApiKeysAsync();
    Task<Result<ApiKeyDto>> GetApiKeyByIdAsync(Guid id);
    Task<Result<ApiKeyDto>> UpdateApiKeyAsync(Guid id, UpdateApiKeyRequest request);
    Task<Result> DeleteApiKeyAsync(Guid id);
    Task<bool> ValidateApiKeyAsync(string apiKey);
    Task UpdateLastUsedAsync(string apiKey);
}
