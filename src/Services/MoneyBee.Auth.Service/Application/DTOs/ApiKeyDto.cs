namespace MoneyBee.Auth.Service.Application.DTOs;

public record ApiKeyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public string MaskedKey { get; init; } = string.Empty; // e.g., "mb_****...****1234"
}