using MoneyBee.Common.DDD;

namespace MoneyBee.Common.ValueObjects;

/// <summary>
/// Value Object for Transaction Code
/// </summary>
public sealed class TransactionCode : ValueObject
{
    public string Value { get; }

    private TransactionCode(string value)
    {
        Value = value;
    }

    public static TransactionCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Transaction code cannot be empty", nameof(value));

        if (value.Length != 10)
            throw new ArgumentException("Transaction code must be 10 characters", nameof(value));

        if (!value.All(char.IsLetterOrDigit))
            throw new ArgumentException("Transaction code must be alphanumeric", nameof(value));

        return new TransactionCode(value.ToUpperInvariant());
    }

    public static TransactionCode Generate()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        return new TransactionCode(code);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(TransactionCode code) => code.Value;
}
