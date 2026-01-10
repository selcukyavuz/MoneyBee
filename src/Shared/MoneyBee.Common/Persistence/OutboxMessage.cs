using System.ComponentModel.DataAnnotations;

namespace MoneyBee.Common.Persistence;

/// <summary>
/// Outbox pattern entity for reliable event publishing
/// Events are stored in database atomically with business data,
/// then published asynchronously by a background processor
/// </summary>
public class OutboxMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type name of the domain event (e.g., "CustomerCreatedDomainEvent")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized event data (JSON)
    /// </summary>
    [Required]
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// When the domain event occurred
    /// </summary>
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the event has been successfully published
    /// </summary>
    public bool Published { get; set; } = false;

    /// <summary>
    /// When the event was published
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Number of processing attempts
    /// </summary>
    public int ProcessAttempts { get; set; } = 0;

    /// <summary>
    /// Last error message if processing failed
    /// </summary>
    [MaxLength(2000)]
    public string? LastError { get; set; }

    /// <summary>
    /// Last processing attempt timestamp
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }
}
