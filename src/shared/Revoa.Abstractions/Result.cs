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

// Constraint removida para permitir também value types (ex.: Result<int>). Comportamento
// idêntico para reference types (todos os usos existentes): Value continua T?, Fail usa default.
public sealed class Result<T>
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

    public static Result<T> Fail(string error) => new(false, error, default);
}
