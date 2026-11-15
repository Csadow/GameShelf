namespace GameShelf.Core;

// A small Result type keeps expected validation errors out of exception handling.
public readonly record struct Result<T>(T? Value, string? Error)
{
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(string error) => new(default, error);

    public Result<U> Map<U>(Func<T, U> transform) =>
        IsSuccess ? Result<U>.Success(transform(Value!)) : Result<U>.Failure(Error!);
}
