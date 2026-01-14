using System.Security.Cryptography;
using MoneyBee.Transfer.Service.Application.Transfers.Services;

namespace MoneyBee.Transfer.Service.Infrastructure.Transfers.Services;

/// <summary>
/// Implementation of transaction code generator
/// Uses cryptographically secure random number generator for unique transaction codes
/// </summary>
public class TransactionCodeGenerator : ITransactionCodeGenerator
{
    /// <summary>
    /// Generates an 8-digit unique transaction code using cryptographically secure random number generation
    /// </summary>
    /// <returns>An 8-digit numeric string (10000000 to 99999999)</returns>
    public string Generate()
    {
        // Generate 8-digit code (10000000 to 99999999)
        // Using RandomNumberGenerator for cryptographically secure random numbers
        var code = RandomNumberGenerator.GetInt32(10000000, 100000000);
        return code.ToString();
    }
}
