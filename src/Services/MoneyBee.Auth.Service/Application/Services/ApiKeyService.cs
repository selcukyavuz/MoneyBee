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

    public async Task<Result<CreateApiKeyResponse>> CreateApiKeyAsync(CreateApiKeyRequest request)
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

        var response = new CreateApiKeyResponse
        {
            Id = created.Id,
            Name = created.Name,
            ApiKey = apiKey, // Only shown once!
            Description = created.Description,
            CreatedAt = created.CreatedAt,
            ExpiresAt = created.ExpiresAt
        };

        return Result<CreateApiKeyResponse>.Success(response);
    }

    public async Task<Result<bool>> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Result<bool>.Validation(ErrorMessages.ApiKey.CannotBeEmpty);
        }

        if (!ApiKeyHelper.IsValidApiKeyFormat(apiKey))
        {
            return Result<bool>.Validation(ErrorMessages.ApiKey.InvalidFormat);
        }

        var keyHash = ApiKeyHelper.HashApiKey(apiKey);
        var key = await repository.GetByKeyHashAsync(keyHash);

        if (key is null)
        {
            return Result<bool>.NotFound(ErrorMessages.ApiKey.NotFound);
        }

        if (!key.IsActive)
        {
            return Result<bool>.Unauthorized(ErrorMessages.ApiKey.Inactive);
        }

        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow)
        {
            return Result<bool>.Unauthorized(ErrorMessages.ApiKey.Expired);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result> UpdateLastUsedAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Result.Validation(ErrorMessages.ApiKey.CannotBeEmpty);
        }

        var keyHash = ApiKeyHelper.HashApiKey(apiKey);
        var key = await repository.GetByKeyHashAsync(keyHash);

        if (key is null)
        {
            return Result.NotFound(ErrorMessages.ApiKey.NotFound);
        }

        key.LastUsedAt = DateTime.UtcNow;
        await repository.UpdateAsync(key);

        logger.LogDebug("API Key last used timestamp updated: {KeyId}", key.Id);
        return Result.Success();
    }
}
