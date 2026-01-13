namespace MoneyBee.Auth.Service.Application.DTOs;

public record UpdateApiKeyRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}
