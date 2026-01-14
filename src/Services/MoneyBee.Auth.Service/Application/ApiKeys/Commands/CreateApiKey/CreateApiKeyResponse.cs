namespace MoneyBee.Auth.Service.Application.ApiKeys.Commands.CreateApiKey;

public record CreateApiKeyResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty; // Only shown once!
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
