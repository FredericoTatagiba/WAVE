namespace WAVE.Domain.Common;

/// <summary>
/// Result of an operation that may fail, avoiding the use of exceptions for flow
/// control (the "railway" pattern). Keeps coupling low and intent clear.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);

    public static Result Failure(string error) => new(false, error);
}

/// <summary>Result that carries a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, string error)
        : base(isSuccess, error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("There is no value in a failure result.");

    public static Result<T> Success(T value) => new(true, value, string.Empty);

    public static new Result<T> Failure(string error) => new(false, default, error);
}
