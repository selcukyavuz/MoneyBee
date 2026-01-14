namespace MoneyBee.Common.Abstractions;

/// <summary>
/// Interface for validating API keys
/// </summary>
public interface IApiKeyValidator
{
    /// <summary>
    /// Validates an API key and returns whether it is valid
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <returns>True if the API key is valid, active, and not expired; otherwise false</returns>
    Task<bool> ValidateApiKeyAsync(string apiKey);
}
