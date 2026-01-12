namespace MoneyBee.Transfer.Service.Helpers;

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
