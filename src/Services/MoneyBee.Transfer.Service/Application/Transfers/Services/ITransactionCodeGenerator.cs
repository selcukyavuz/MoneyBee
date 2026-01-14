namespace MoneyBee.Transfer.Service.Application.Transfers.Services;

/// <summary>
/// Service for generating unique transaction codes
/// </summary>
public interface ITransactionCodeGenerator
{
    /// <summary>
    /// Generates an 8-digit unique transaction code
    /// </summary>
    /// <returns>An 8-digit numeric string (10000000 to 99999999)</returns>
    string Generate();
}
