namespace MoneyBee.Transfer.Service.Helpers;

public static class TransactionCodeGenerator
{
    private static readonly Random _random = new Random();

    /// <summary>
    /// Generates an 8-digit unique transaction code
    /// </summary>
    public static string Generate()
    {
        // Generate 8-digit code (10000000 to 99999999)
        var code = _random.Next(10000000, 100000000);
        return code.ToString();
    }

    public static bool IsValid(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        if (code.Length != 8)
            return false;

        return code.All(char.IsDigit);
    }
}

public static class FeeCalculator
{
    private const decimal BaseFee = 5.00m; // 5 TRY base fee
    private const decimal FeePercentage = 0.01m; // 1% of amount

    public static decimal Calculate(decimal amountInTRY)
    {
        var percentageFee = amountInTRY * FeePercentage;
        var totalFee = BaseFee + percentageFee;

        // Round to 2 decimal places
        return Math.Round(totalFee, 2);
    }
}
