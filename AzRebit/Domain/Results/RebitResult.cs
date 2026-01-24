using AzRebit.Domain.Exceptions;


namespace AzRebit.Domain.Results;

/// <summary>
/// Non-generic version for operations without data payload
/// </summary>
public class RebitResult
{
    /// <summary>
    /// Indicates wheter the operation was successfull
    /// </summary>
    public bool IsSuccess { get; init; }
    /// <summary>
    /// Message returned from the operation
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Error type from AzRebit lib
    /// </summary>
    public AzRebitErrorType ErrorType { get; init; }

    /// <summary>
    /// Creates a new instance of <see cref="RebitResult"/> that represents a successful result.
    /// </summary>
    /// <returns>A <see cref="RebitResult"/> instance with <see cref="RebitResult.IsSuccess"/> set to <see
    /// langword="true"/>.</returns>
    public static RebitResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a successful result with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the successful outcome. Can be null or empty if no message is required.</param>
    /// <returns>A <see cref="RebitResult"/> instance representing a successful result with the provided message.</returns>
    public static RebitResult Success(string message) => new()
    {
        IsSuccess = true,
        Message = message
    };

    /// <summary>
    /// Creates a new result that represents a failed action.
    /// </summary>
    /// <returns>A <see cref="RebitResult"/> instance with <c>IsSuccess</c> set to <see langword="false"/>.</returns>
    public static RebitResult Failure() => new() { IsSuccess = false };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="errorType"></param>
    /// <returns></returns>
    public static RebitResult Failure(string message, AzRebitErrorType errorType = AzRebitErrorType.UnexpectedError) => new()
    {
        IsSuccess = false,
        Message = message,
        ErrorType = errorType
    };
}

/// <summary>
/// Represents the result of an operation, optionally with a data payload
/// </summary>
/// <typeparam name="T">The type of the data payload (use object for no specific data)</typeparam>
public class  RebitResult<T> : RebitResult
{
    /// <summary>
    /// The data payload of the operation
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful operation result with data
    /// </summary>
    public static RebitResult<T> Success(T data) => new RebitResult<T>
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a successful operation result with data and message
    /// </summary>
    public static RebitResult<T> Success(T data, string message) => new RebitResult<T>
    {
        IsSuccess = true,
        Data = data,
        Message = message
    };

    /// <summary>
    /// Creates a successful operation result without data
    /// </summary>
    public static new RebitResult<T> Success() => new RebitResult<T> { IsSuccess = true };

    /// <summary>
    /// Creates a successful operation result without data but with message
    /// </summary>
    public static new RebitResult<T> Success(string message) => new RebitResult<T>
    {
        IsSuccess = true,
        Message = message
    };

    /// <summary>
    /// Creates a failed operation result
    /// </summary>
    public static new RebitResult<T> Failure() => new() { IsSuccess = false };

    /// <summary>
    /// Creates a failed operation result with a message
    /// </summary>
    public static new RebitResult<T> Failure(string message, AzRebitErrorType errorType = AzRebitErrorType.UnexpectedError) => new RebitResult<T>
    {
        IsSuccess = false,
        Message = message,
        ErrorType = errorType
    };

    /// <summary>
    /// Implicit conversion from the data type to a successful operation result
    /// </summary>
    public static implicit operator RebitResult<T>(T data) => Success(data);
}

