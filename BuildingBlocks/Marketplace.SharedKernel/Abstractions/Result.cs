namespace Marketplace.SharedKernel.Abstractions;

/// <summary>
/// Represents the result of an operation without a return value.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new result instance.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation succeeded.</param>
    /// <param name="error">The failure message when the operation does not succeed.</param>
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message when the operation fails.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result instance.</returns>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The failure reason.</param>
    /// <returns>A failed result instance.</returns>
    public static Result Failure(string error) => new(false, error);
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the returned value.</typeparam>
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, string? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the operation value when the result is successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value returned by the operation.</param>
    /// <returns>A successful result instance.</returns>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result with no value.
    /// </summary>
    /// <param name="error">The failure reason.</param>
    /// <returns>A failed result instance.</returns>
    public static new Result<T> Failure(string error) => new(false, default, error);
}