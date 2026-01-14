using MoneyBee.Auth.Service.Domain.ApiKeys;
using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Results;

namespace MoneyBee.Auth.Service.Application.ApiKeys.Commands.UpdateApiKey;

/// <summary>
/// Handles updating API key's last used timestamp
/// </summary>
public class UpdateApiKeyLastUsedHandler(
    IApiKeyRepository repository,
    ILogger<UpdateApiKeyLastUsedHandler> logger) : ICommandHandler<string, Result>
{
    public async Task<Result> HandleAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        // 1. Validate input
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Result.Validation(ApiKeyErrorMessages.CannotBeEmpty);
        }

        // 2. Get API key by hash
        var keyHash = ApiKeyHelper.HashApiKey(apiKey);
        var key = await repository.GetByKeyHashAsync(keyHash);

        if (key is null)
        {
            return Result.NotFound(ApiKeyErrorMessages.NotFound);
        }

        // 3. Update last used timestamp
        key.LastUsedAt = DateTime.UtcNow;
        await repository.UpdateAsync(key);

        logger.LogDebug("API Key last used timestamp updated: {KeyId}", key.Id);
        
        return Result.Success();
    }
}
