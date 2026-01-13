namespace MoneyBee.Transfer.Service.Helpers;

/// <summary>
/// Helper class for generating unique transaction codes for money transfers
/// </summary>
public static class TransactionCodeGenerator
{
    private static readonly Random _random = new Random();

    /// <summary>
    /// Generates an 8-digit unique transaction code
    /// </summary>
    /// <returns>An 8-digit numeric string (10000000 to 99999999)</returns>
    public static string Generate()
    {
        // Generate 8-digit code (10000000 to 99999999)
        var code = _random.Next(10000000, 100000000);
        return code.ToString();
    }
}
