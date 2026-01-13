namespace MoneyBee.Transfer.Service.Helpers;

/// <summary>
/// Helper class for calculating transaction fees
/// Fee structure: Base fee + percentage of transfer amount
/// </summary>
public static class FeeCalculator
{
    /// <summary>
    /// Calculates the transaction fee based on transfer amount
    /// </summary>
    /// <param name="amountInTRY">The transfer amount in TRY</param>
    /// <param name="baseFee">Base fee to apply</param>
    /// <param name="feePercentage">Percentage fee (e.g., 0.01 for 1%)</param>
    /// <returns>Total fee rounded to 2 decimal places (Base fee + percentage of amount)</returns>
    public static decimal Calculate(decimal amountInTRY, decimal baseFee, decimal feePercentage)
    {
        var percentageFee = amountInTRY * feePercentage;
        var totalFee = baseFee + percentageFee;

        // Round to 2 decimal places
        return Math.Round(totalFee, 2);
    }
}
