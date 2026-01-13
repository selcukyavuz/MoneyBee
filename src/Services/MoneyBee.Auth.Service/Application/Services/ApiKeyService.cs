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

    public async Task<IEnumerable<ApiKeyDto>> GetAllApiKeysAsync()
    {
        var keys = await repository.GetAllAsync();

        return keys.Select(k => new ApiKeyDto
        {
            Id = k.Id,
            Name = k.Name,
            Description = k.Description,
            IsActive = k.IsActive,
            CreatedAt = k.CreatedAt,
            ExpiresAt = k.ExpiresAt,
            LastUsedAt = k.LastUsedAt,
            MaskedKey = ApiKeyHelper.MaskApiKey($"mb_{k.KeyHash[..Math.Min(28, k.KeyHash.Length)]}")
        });
    }

    public async Task<Result<ApiKeyDto>> GetApiKeyByIdAsync(Guid id)
    {
        var key = await repository.GetByIdAsync(id);

        if (key is null)
        {
            return Result<ApiKeyDto>.Failure("API Key not found");
        }

        return Result<ApiKeyDto>.Success(new ApiKeyDto
        {
            Id = key.Id,
            Name = key.Name,
            Description = key.Description,
            IsActive = key.IsActive,
            CreatedAt = key.CreatedAt,
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt,
            MaskedKey = ApiKeyHelper.MaskApiKey($"mb_{key.KeyHash[..Math.Min(28, key.KeyHash.Length)]}")
        });
    }

    public async Task<Result<ApiKeyDto>> UpdateApiKeyAsync(Guid id, UpdateApiKeyRequest request)
    {
        var key = await repository.GetByIdAsync(id);

        if (key is null)
        {
            return Result<ApiKeyDto>.Failure(ErrorMessages.ApiKey.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            key.Name = request.Name;
        }

        if (request.Description != null)
        {
            key.Description = request.Description;
        }

        if (request.IsActive.HasValue)
        {
            key.IsActive = request.IsActive.Value;
        }

        var updated = await repository.UpdateAsync(key);

        logger.LogInformation("API Key updated: {KeyId}", id);

        return Result<ApiKeyDto>.Success(new ApiKeyDto
        {
            Id = updated.Id,
            Name = updated.Name,
            Description = updated.Description,
            IsActive = updated.IsActive,
            CreatedAt = updated.CreatedAt,
            ExpiresAt = updated.ExpiresAt,
            LastUsedAt = updated.LastUsedAt,
            MaskedKey = ApiKeyHelper.MaskApiKey($"mb_{updated.KeyHash.Substring(0, Math.Min(28, updated.KeyHash.Length))}")
        });
    }

    public async Task<Result> DeleteApiKeyAsync(Guid id)
    {
        var key = await repository.GetByIdAsync(id);

        if (key is null)
        {
            return Result.Failure(ErrorMessages.ApiKey.NotFound);
        }

        var deleted = await repository.DeleteAsync(id);

        if (deleted)
        {
            logger.LogWarning("API Key deleted: {KeyId} - {KeyName}", id, key.Name);
        }

        return Result.Success();
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
