using MoneyBee.Auth.Service.Domain.Entities;

namespace MoneyBee.Auth.Service.Domain.Interfaces;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid id);
    Task<ApiKey?> GetByKeyHashAsync(string keyHash);
    Task<IEnumerable<ApiKey>> GetAllAsync();
    Task<ApiKey> CreateAsync(ApiKey apiKey);
    Task<ApiKey> UpdateAsync(ApiKey apiKey);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
