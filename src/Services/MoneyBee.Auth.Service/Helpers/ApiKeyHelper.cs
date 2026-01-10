using System.Security.Cryptography;
using System.Text;

namespace MoneyBee.Auth.Service.Helpers;

public static class ApiKeyHelper
{
    private const string Prefix = "mb_";
    private const int KeyLength = 32;

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
            .Substring(0, KeyLength);

        return $"{Prefix}{key}";
    }

    public static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 12)
            return "****";

        var prefix = apiKey.Substring(0, 3); // "mb_"
        var suffix = apiKey.Substring(apiKey.Length - 4); // last 4 chars
        return $"{prefix}****...****{suffix}";
    }

    public static bool IsValidApiKeyFormat(string apiKey)
    {
        return !string.IsNullOrEmpty(apiKey) && 
               apiKey.StartsWith(Prefix) && 
               apiKey.Length >= 35;
    }
}
