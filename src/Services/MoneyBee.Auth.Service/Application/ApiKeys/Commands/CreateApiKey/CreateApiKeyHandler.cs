using MoneyBee.Auth.Service.Domain.ApiKeys;
using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Results;

namespace MoneyBee.Auth.Service.Application.ApiKeys.Commands.CreateApiKey;

/// <summary>
/// Handles API key creation
/// </summary>
public class CreateApiKeyHandler(
    IApiKeyRepository repository,
    ILogger<CreateApiKeyHandler> logger) : ICommandHandler<CreateApiKeyRequest, Result<CreateApiKeyResponse>>
{
    public async Task<Result<CreateApiKeyResponse>> HandleAsync(CreateApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<CreateApiKeyResponse>.Validation("Name is required");

        if (request.Name.Length > 100)
            return Result<CreateApiKeyResponse>.Validation("Name cannot exceed 100 characters");

        if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 500)
            return Result<CreateApiKeyResponse>.Validation("Description cannot exceed 500 characters");

        if (request.ExpiresInDays.HasValue)
        {
            if (request.ExpiresInDays.Value <= 0)
                return Result<CreateApiKeyResponse>.Validation("ExpiresInDays must be greater than 0");

            if (request.ExpiresInDays.Value > 3650)
                return Result<CreateApiKeyResponse>.Validation("ExpiresInDays cannot exceed 3650 (10 years)");
        }

        // 2. Generate API key and hash
        var apiKey = ApiKeyHelper.GenerateApiKey();
        var keyHash = ApiKeyHelper.HashApiKey(apiKey);

        // 3. Create entity
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

        // 4. Save
        var created = await repository.CreateAsync(entity);

        logger.LogInformation("API Key created: {KeyId} - {KeyName}", created.Id, created.Name);

        // 5. Return response with plain API key (only shown once!)
        var response = new CreateApiKeyResponse
        {
            Id = created.Id,
            Name = created.Name,
            ApiKey = apiKey,
            Description = created.Description,
            CreatedAt = created.CreatedAt,
            ExpiresAt = created.ExpiresAt
        };

        return Result<CreateApiKeyResponse>.Success(response);
    }
}
