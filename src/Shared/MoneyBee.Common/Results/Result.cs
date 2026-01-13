namespace MoneyBee.Common.Results;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// Use instead of throwing exceptions for expected validation failures.
/// </summary>
public readonly struct Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ResultErrorType ErrorType { get; }

    private Result(bool isSuccess, string? error, ResultErrorType errorType = ResultErrorType.Failure)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error, ResultErrorType errorType = ResultErrorType.Failure) 
        => new(false, error, errorType);
    
    public static Result NotFound(string error) => new(false, error, ResultErrorType.NotFound);
    public static Result Validation(string error) => new(false, error, ResultErrorType.Validation);
    public static Result Unauthorized(string error) => new(false, error, ResultErrorType.Unauthorized);
    public static Result Forbidden(string error) => new(false, error, ResultErrorType.Forbidden);
    public static Result Conflict(string error) => new(false, error, ResultErrorType.Conflict);

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
    public ResultErrorType ErrorType { get; }

    private Result(bool isSuccess, T? value, string? error, ResultErrorType errorType = ResultErrorType.Failure)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error, ResultErrorType errorType = ResultErrorType.Failure) 
        => new(false, default, error, errorType);
    
    public static Result<T> NotFound(string error) => new(false, default, error, ResultErrorType.NotFound);
    public static Result<T> Validation(string error) => new(false, default, error, ResultErrorType.Validation);
    public static Result<T> Unauthorized(string error) => new(false, default, error, ResultErrorType.Unauthorized);
    public static Result<T> Forbidden(string error) => new(false, default, error, ResultErrorType.Forbidden);
    public static Result<T> Conflict(string error) => new(false, default, error, ResultErrorType.Conflict);

    public T GetValueOrThrow()
    {
        if (!IsSuccess)
            throw new InvalidOperationException(Error);
        return Value!;
    }
}
