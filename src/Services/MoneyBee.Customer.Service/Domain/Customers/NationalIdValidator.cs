namespace MoneyBee.Customer.Service.Domain.Customers;

/// <summary>
/// Helper class for validating and normalizing Turkish National ID (TC Kimlik No)
/// </summary>
public static class NationalIdValidator
{
    /// <summary>
    /// Validates Turkish National ID (TC Kimlik No) using the official algorithm
    /// </summary>
    /// <param name="nationalId">The 11-digit national ID to validate</param>
    /// <returns>True if valid according to Turkish ID algorithm, false otherwise</returns>
    public static bool IsValid(string nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
            return false;

        // Must be 11 digits
        if (nationalId.Length != 11)
            return false;

        // Must be all digits
        if (!nationalId.All(char.IsDigit))
            return false;

        // First digit cannot be 0
        if (nationalId[0] == '0')
            return false;

        var digits = nationalId.Select(c => int.Parse(c.ToString())).ToArray();

        // Sum of first 10 digits mod 10 must equal 11th digit
        var sumFirst10 = digits.Take(10).Sum();
        if (sumFirst10 % 10 != digits[10])
            return false;

        // (sum of 1st, 3rd, 5th, 7th, 9th digits * 7 - sum of 2nd, 4th, 6th, 8th digits) mod 10 must equal 10th digit
        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        var checkDigit = (oddSum * 7 - evenSum) % 10;
        
        if (checkDigit < 0)
            checkDigit += 10;

        return checkDigit == digits[9];
    }

    public static string Normalize(string nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
            return string.Empty;

        return new string(nationalId.Where(char.IsDigit).ToArray());
    }
}
