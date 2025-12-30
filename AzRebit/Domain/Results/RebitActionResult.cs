using AzRebit.Domain.Exceptions;


namespace AzRebit.Domain.Results;

/// <summary>
/// Non-generic version for operations without data payload
/// </summary>
public class RebitActionResult
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
    /// Creates a new instance of <see cref="RebitActionResult"/> that represents a successful result.
    /// </summary>
    /// <returns>A <see cref="RebitActionResult"/> instance with <see cref="RebitActionResult.IsSuccess"/> set to <see
    /// langword="true"/>.</returns>
    public static RebitActionResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a successful result with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the successful outcome. Can be null or empty if no message is required.</param>
    /// <returns>A <see cref="RebitActionResult"/> instance representing a successful result with the provided message.</returns>
    public static RebitActionResult Success(string message) => new()
    {
        IsSuccess = true,
        Message = message
    };

    /// <summary>
    /// Creates a new result that represents a failed action.
    /// </summary>
    /// <returns>A <see cref="RebitActionResult"/> instance with <c>IsSuccess</c> set to <see langword="false"/>.</returns>
    public static RebitActionResult Failure() => new() { IsSuccess = false };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="errorType"></param>
    /// <returns></returns>
    public static RebitActionResult Failure(string message, AzRebitErrorType errorType = AzRebitErrorType.UnexpectedError) => new()
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
public class RebitActionResult<T> : RebitActionResult
{
    /// <summary>
    /// The data payload of the operation
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful operation result with data
    /// </summary>
    public static RebitActionResult<T> Success(T data) => new RebitActionResult<T>
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a successful operation result with data and message
    /// </summary>
    public static RebitActionResult<T> Success(T data, string message) => new RebitActionResult<T>
    {
        IsSuccess = true,
        Data = data,
        Message = message
    };

    /// <summary>
    /// Creates a successful operation result without data
    /// </summary>
    public static new RebitActionResult<T> Success() => new RebitActionResult<T> { IsSuccess = true };

    /// <summary>
    /// Creates a successful operation result without data but with message
    /// </summary>
    public static new RebitActionResult<T> Success(string message) => new RebitActionResult<T>
    {
        IsSuccess = true,
        Message = message
    };

    /// <summary>
    /// Creates a failed operation result
    /// </summary>
    public static new RebitActionResult<T> Failure() => new RebitActionResult<T> { IsSuccess = false };

    /// <summary>
    /// Creates a failed operation result with a message
    /// </summary>
    public static new RebitActionResult<T> Failure(string message, AzRebitErrorType errorType = AzRebitErrorType.UnexpectedError) => new RebitActionResult<T>
    {
        IsSuccess = false,
        Message = message,
        ErrorType = errorType
    };

    /// <summary>
    /// Implicit conversion from the data type to a successful operation result
    /// </summary>
    public static implicit operator RebitActionResult<T>(T data) => Success(data);
}

