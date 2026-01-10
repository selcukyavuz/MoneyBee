namespace MoneyBee.Auth.Service.Application.DTOs;

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ExpiresInDays { get; set; }
}

public class CreateApiKeyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty; // Only shown once!
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ApiKeyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string MaskedKey { get; set; } = string.Empty; // e.g., "mb_****...****1234"
}

public class UpdateApiKeyRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
