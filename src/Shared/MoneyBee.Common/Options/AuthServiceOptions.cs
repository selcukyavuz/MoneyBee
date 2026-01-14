namespace MoneyBee.Common.Options;

/// <summary>
/// Options for Auth Service integration
/// </summary>
public class AuthServiceOptions
{
    /// <summary>
    /// Base URL for Auth Service (e.g., http://localhost:5001)
    /// </summary>
    public string Url { get; set; } = "http://localhost:5001";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
