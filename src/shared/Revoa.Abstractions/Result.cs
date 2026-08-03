namespace Revoa.Abstractions;

public sealed class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool success, string? error)
    {
        IsSuccess = success;
        Error = error;
    }

    public static Result Ok() => new(true, null);

    public static Result Fail(string error) => new(false, error);
}

public sealed class Result<T> where T : class
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public T? Value { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool success, string? error, T? value)
    {
        IsSuccess = success;
        Error = error;
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, null, value);

    public static Result<T> Fail(string error) => new(false, error, null);
}
