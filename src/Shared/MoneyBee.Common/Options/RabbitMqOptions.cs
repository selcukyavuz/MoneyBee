namespace MoneyBee.Common.Options;

/// <summary>
/// Options for RabbitMQ connection
/// </summary>
public class RabbitMqOptions
{
    /// <summary>
    /// RabbitMQ Host (default: localhost)
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ Username (default: moneybee)
    /// </summary>
    public string Username { get; set; } = "moneybee";

    /// <summary>
    /// RabbitMQ Password (default: moneybee123)
    /// </summary>
    public string Password { get; set; } = "moneybee123";

    /// <summary>
    /// Enable automatic recovery (default: true)
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Network recovery interval in seconds (default: 10)
    /// </summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;
}
