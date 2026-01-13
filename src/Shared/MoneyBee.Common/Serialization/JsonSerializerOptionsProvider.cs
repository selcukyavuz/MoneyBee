using System.Text.Json;
using System.Text.Json.Serialization;

namespace MoneyBee.Common.Serialization;

/// <summary>
/// Provides pre-configured JsonSerializerOptions instances for consistent JSON serialization across the application
/// </summary>
public static class JsonSerializerOptionsProvider
{
    /// <summary>
    /// Default JsonSerializerOptions with case-insensitive property matching and enum string conversion
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// JsonSerializerOptions optimized for web APIs with case-insensitive matching and camelCase naming
    /// </summary>
    public static JsonSerializerOptions Web { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
