using MoneyBee.Common.DDD;

namespace MoneyBee.Common.ValueObjects;

/// <summary>
/// Value Object for National ID with validation
/// </summary>
public sealed class NationalId : ValueObject
{
    public string Value { get; }

    private NationalId(string value)
    {
        Value = value;
    }

    public static NationalId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("National ID cannot be empty", nameof(value));

        var normalized = Normalize(value);
        
        if (!IsValid(normalized))
            throw new ArgumentException("Invalid National ID format", nameof(value));

        return new NationalId(normalized);
    }

    public static string Normalize(string nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
            return string.Empty;

        return new string(nationalId.Where(char.IsDigit).ToArray());
    }

    public static bool IsValid(string nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
            return false;

        var normalized = Normalize(nationalId);

        if (normalized.Length != 11)
            return false;

        if (!normalized.All(char.IsDigit))
            return false;

        if (normalized[0] == '0')
            return false;

        var digits = normalized.Select(c => int.Parse(c.ToString())).ToArray();

        var sumOdd = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var sumEven = digits[1] + digits[3] + digits[5] + digits[7];

        var digit10 = ((sumOdd * 7) - sumEven) % 10;
        if (digit10 != digits[9])
            return false;

        var sumFirst10 = digits.Take(10).Sum();
        var digit11 = sumFirst10 % 10;
        if (digit11 != digits[10])
            return false;

        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(NationalId nationalId) => nationalId.Value;
}
