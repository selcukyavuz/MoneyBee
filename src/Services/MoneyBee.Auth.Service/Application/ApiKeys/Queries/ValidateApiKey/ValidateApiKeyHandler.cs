using MoneyBee.Auth.Service.Domain.ApiKeys;
using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Results;

namespace MoneyBee.Auth.Service.Application.ApiKeys.Queries.ValidateApiKey;

/// <summary>
/// Handles API key validation
/// </summary>
public class ValidateApiKeyHandler(
    IApiKeyRepository repository) : IQueryHandler<string, Result<bool>>
{
    public async Task<Result<bool>> HandleAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        // 1. Validate format
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Result<bool>.Validation(ApiKeyErrorMessages.CannotBeEmpty);
        }

        if (!ApiKeyHelper.IsValidApiKeyFormat(apiKey))
        {
            return Result<bool>.Validation(ApiKeyErrorMessages.InvalidFormat);
        }

        // 2. Get API key by hash
        var keyHash = ApiKeyHelper.HashApiKey(apiKey);
        var key = await repository.GetByKeyHashAsync(keyHash);

        if (key is null)
        {
            return Result<bool>.NotFound(ApiKeyErrorMessages.NotFound);
        }

        // 3. Check if active
        if (!key.IsActive)
        {
            return Result<bool>.Unauthorized(ApiKeyErrorMessages.Inactive);
        }

        // 4. Check if expired
        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow)
        {
            return Result<bool>.Unauthorized(ApiKeyErrorMessages.Expired);
        }

        return Result<bool>.Success(true);
    }
}
