namespace MoneyBee.Common.Constants;

/// <summary>
/// Constants for API Key authentication middleware
/// </summary>
public static class ApiKeyAuthenticationMiddlewareConstants
{
    // Paths that bypass API key authentication
    public const string HealthCheckPath = "/health";
    public const string SwaggerPathPrefix = "/swagger";
    public const string AuthServiceKeysPath = "/api/auth/keys";
    public const string ApiKeysPath = "/apikeys";

    // HTTP methods
    public const string PostMethod = "POST";

    // API Key format validation
    public const string ApiKeyPrefix = "mb_";
    public const int MinApiKeyLength = 20;

    // Content types
    public const string JsonContentType = "application/json";

    // Error messages
    public const string ApiKeyMissingError = "API Key is missing";
    public const string InvalidApiKeyFormatError = "Invalid API Key format";
    public const string InvalidOrExpiredApiKeyError = "Invalid or expired API Key";

    // Log messages
    public const string ApiKeyMissingLogMessage = "API Key missing from request to {Path} from {IpAddress}";
    public const string InvalidFormatLogMessage = "Invalid API Key format from {IpAddress}";
    public const string InvalidAttemptLogMessage = "Invalid API Key attempt from {IpAddress} for path {Path}";
    public const string ValidatedSuccessfullyLogMessage = "API Key validated successfully for path {Path}";
}
