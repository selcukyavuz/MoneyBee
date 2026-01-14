namespace MoneyBee.Auth.Service.Domain.ApiKeys;

/// <summary>
/// Error message constants for API Key domain
/// </summary>
public static class ApiKeyErrorMessages
{
    public const string NotFound = "API Key not found";
    public const string CannotBeEmpty = "API Key cannot be empty";
    public const string InvalidFormat = "Invalid API Key format";
    public const string Inactive = "API Key is inactive";
    public const string Expired = "API Key has expired";
}
