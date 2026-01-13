using MoneyBee.Auth.Service.Application.DTOs;
using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Auth.Service.Constants;
using MoneyBee.Auth.Service.Domain.Entities;
using MoneyBee.Auth.Service.Domain.Interfaces;
using MoneyBee.Auth.Service.Helpers;
using MoneyBee.Common.Results;

namespace MoneyBee.Auth.Service.Application.Services;

public class ApiKeyService(
    IApiKeyRepository repository,
    ILogger<ApiKeyService> logger) : IApiKeyService
{

    public async Task<CreateApiKeyResponse> CreateApiKeyAsync(CreateApiKeyRequest request)
    {
        var apiKey = ApiKeyHelper.GenerateApiKey();
        var keyHash = ApiKeyHelper.HashApiKey(apiKey);

        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            KeyHash = keyHash,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresInDays.HasValue 
                ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value) 
                : null
        };

        var created = await repository.CreateAsync(entity);

        logger.LogInformation("API Key created: {KeyId} - {KeyName}", created.Id, created.Name);

        return new CreateApiKeyResponse
        {
            Id = created.Id,
            Name = created.Name,
            ApiKey = apiKey, // Only shown once!
            Description = created.Description,
            CreatedAt = created.CreatedAt,
            ExpiresAt = created.ExpiresAt
        };
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || !ApiKeyHelper.IsValidApiKeyFormat(apiKey))
        {
            return false;
        }

        var keyHash = ApiKeyHelper.HashApiKey(apiKey);
        
        var key = await repository.GetByKeyHashAsync(keyHash);

        if (key is null || !key.IsActive)
        {
            return false;
        }

        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    public async Task UpdateLastUsedAsync(string apiKey)
    {
        var keyHash = ApiKeyHelper.HashApiKey(apiKey);
        var key = await repository.GetByKeyHashAsync(keyHash);

        if (key is not null)
        {
            key.LastUsedAt = DateTime.UtcNow;
            await repository.UpdateAsync(key);
        }
    }
}
