using System.Diagnostics;
using MoneyBee.Auth.Service.Application.DTOs;
using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Auth.Service.Domain.Entities;
using MoneyBee.Auth.Service.Domain.Interfaces;
using MoneyBee.Auth.Service.Helpers;

namespace MoneyBee.Auth.Service.Application.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _repository;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(
        IApiKeyRepository repository,
        ILogger<ApiKeyService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

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

        var created = await _repository.CreateAsync(entity);

        _logger.LogInformation("API Key created: {KeyId} - {KeyName}", created.Id, created.Name);

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
        var keys = await _repository.GetAllAsync();

        return keys.Select(k => new ApiKeyDto
        {
            Id = k.Id,
            Name = k.Name,
            Description = k.Description,
            IsActive = k.IsActive,
            CreatedAt = k.CreatedAt,
            ExpiresAt = k.ExpiresAt,
            LastUsedAt = k.LastUsedAt,
            MaskedKey = ApiKeyHelper.MaskApiKey($"mb_{k.KeyHash.Substring(0, Math.Min(28, k.KeyHash.Length))}")
        });
    }

    public async Task<ApiKeyDto?> GetApiKeyByIdAsync(Guid id)
    {
        var key = await _repository.GetByIdAsync(id);

        if (key == null)
            return null;

        return new ApiKeyDto
        {
            Id = key.Id,
            Name = key.Name,
            Description = key.Description,
            IsActive = key.IsActive,
            CreatedAt = key.CreatedAt,
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt,
            MaskedKey = ApiKeyHelper.MaskApiKey($"mb_{key.KeyHash.Substring(0, Math.Min(28, key.KeyHash.Length))}")
        };
    }

    public async Task<ApiKeyDto?> UpdateApiKeyAsync(Guid id, UpdateApiKeyRequest request)
    {
        var key = await _repository.GetByIdAsync(id);

        if (key == null)
            return null;

        if (!string.IsNullOrWhiteSpace(request.Name))
            key.Name = request.Name;

        if (request.Description != null)
            key.Description = request.Description;

        if (request.IsActive.HasValue)
            key.IsActive = request.IsActive.Value;

        var updated = await _repository.UpdateAsync(key);

        _logger.LogInformation("API Key updated: {KeyId}", id);

        return new ApiKeyDto
        {
            Id = updated.Id,
            Name = updated.Name,
            Description = updated.Description,
            IsActive = updated.IsActive,
            CreatedAt = updated.CreatedAt,
            ExpiresAt = updated.ExpiresAt,
            LastUsedAt = updated.LastUsedAt,
            MaskedKey = ApiKeyHelper.MaskApiKey($"mb_{updated.KeyHash.Substring(0, Math.Min(28, updated.KeyHash.Length))}")
        };
    }

    public async Task<bool> DeleteApiKeyAsync(Guid id)
    {
        var key = await _repository.GetByIdAsync(id);

        if (key == null)
            return false;

        var deleted = await _repository.DeleteAsync(id);

        if (deleted)
        {
            _logger.LogWarning("API Key deleted: {KeyId} - {KeyName}", id, key.Name);
        }

        return deleted;
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        var stopwatch = Stopwatch.StartNew();
        
        if (string.IsNullOrWhiteSpace(apiKey) || !ApiKeyHelper.IsValidApiKeyFormat(apiKey))
        {
            stopwatch.Stop();
            return false;
        }

        var keyHash = ApiKeyHelper.HashApiKey(apiKey);
        
        var key = await _repository.GetByKeyHashAsync(keyHash);

        if (key == null || !key.IsActive)
        {
            stopwatch.Stop();
            return false;
        }

        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow)
        {
            stopwatch.Stop();
            return false;
        }

        stopwatch.Stop();
        return true;
    }

    public async Task UpdateLastUsedAsync(string apiKey)
    {
        var keyHash = ApiKeyHelper.HashApiKey(apiKey);
        var key = await _repository.GetByKeyHashAsync(keyHash);

        if (key != null)
        {
            key.LastUsedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(key);
        }
    }
}
