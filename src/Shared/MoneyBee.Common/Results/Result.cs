namespace MoneyBee.Common.Results;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// Use instead of throwing exceptions for expected validation failures.
/// </summary>
public readonly struct Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public void ThrowIfFailure()
    {
        if (!IsSuccess)
            throw new InvalidOperationException(Error);
    }
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);

    public T GetValueOrThrow()
    {
        if (!IsSuccess)
            throw new InvalidOperationException(Error);
        return Value!;
    }
}
