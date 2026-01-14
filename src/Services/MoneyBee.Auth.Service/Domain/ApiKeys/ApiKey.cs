namespace MoneyBee.Auth.Service.Domain.ApiKeys;

/// <summary>
/// Represents an API Key entity for authentication and authorization
/// </summary>
public class ApiKey
{
    /// <summary>
    /// Gets or sets the unique identifier of the API key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the API key
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SHA256 hash of the API key
    /// </summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the API key is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the expiration timestamp (optional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the last used timestamp for tracking
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the API key
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Updates the last used timestamp to current UTC time
    /// </summary>
    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }
}
