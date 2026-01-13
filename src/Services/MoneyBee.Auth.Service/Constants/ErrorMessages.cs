namespace MoneyBee.Auth.Service.Constants;

/// <summary>
/// Error message constants for Auth Service
/// </summary>
public static class ErrorMessages
{
    public static class ApiKey
    {
        public const string NotFound = "API Key not found";
        public const string CannotBeEmpty = "API Key cannot be empty";
        public const string InvalidFormat = "Invalid API Key format";
        public const string Inactive = "API Key is inactive";
        public const string Expired = "API Key has expired";
    }
}
