namespace MoneyBee.Auth.Service.Application.DTOs;

public record CreateApiKeyRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? ExpiresInDays { get; init; }
}
