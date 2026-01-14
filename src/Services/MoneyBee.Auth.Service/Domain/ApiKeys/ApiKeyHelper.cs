using System.Security.Cryptography;
using System.Text;

namespace MoneyBee.Auth.Service.Domain.ApiKeys;

/// <summary>
/// Helper class for API Key generation, hashing, and validation
/// </summary>
public static class ApiKeyHelper
{
    private const string Prefix = "mb_";
    private const int KeyLength = 32;

    /// <summary>
    /// Generates a new random API key with "mb_" prefix
    /// </summary>
    /// <returns>A base64-encoded API key with prefix (35 characters total)</returns>
    public static string GenerateApiKey()
    {
        var randomBytes = new byte[KeyLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var key = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            [..KeyLength];

        return $"{Prefix}{key}";
    }

    /// <summary>
    /// Hashes an API key using SHA256 algorithm
    /// </summary>
    /// <param name="apiKey">The API key to hash</param>
    /// <returns>SHA256 hash of the API key as hexadecimal string</returns>
    public static string HashApiKey(string apiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 12)
            return "****";

        var prefix = apiKey[..3]; // "mb_"
        var suffix = apiKey[^4..]; // last 4 chars
        return $"{prefix}****...****{suffix}";
    }

    public static bool IsValidApiKeyFormat(string apiKey)
    {
        return !string.IsNullOrEmpty(apiKey) && 
               apiKey.StartsWith(Prefix) && 
               apiKey.Length >= 35;
    }
}
