using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Shared.Model;

/// <summary>
/// Represents the result of an operation without a data payload
/// </summary>
public class ActionResult
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Optional message describing the result
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Creates a successful operation result
    /// </summary>
    public static ActionResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a successful operation result with a message
    /// </summary>
    public static ActionResult Success(string message) => new() { IsSuccess = true, Message = message };

    /// <summary>
    /// Creates a failed operation result
    /// </summary>
    public static ActionResult Failure() => new() { IsSuccess = false };

    /// <summary>
    /// Creates a failed operation result with a message
    /// </summary>
    public static ActionResult Failure(string message) => new() { IsSuccess = false, Message = message };
}

/// <summary>
/// Represents the result of an operation with a data payload
/// </summary>
/// <typeparam name="T">The type of the data payload</typeparam>
public class OperationResult<T> : ActionResult
{
    /// <summary>
    /// The data payload of the operation
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful operation result with data
    /// </summary>
    public static OperationResult<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a successful operation result with data and message
    /// </summary>
    public static OperationResult<T> Success(T data, string message) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message
    };

    /// <summary>
    /// Creates a successful operation result without data
    /// </summary>
    public static new OperationResult<T> Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a successful operation result without data but with message
    /// </summary>
    public static new OperationResult<T> Success(string message) => new() { IsSuccess = true, Message = message };

    /// <summary>
    /// Creates a failed operation result
    /// </summary>
    public static new OperationResult<T> Failure() => new() { IsSuccess = false };

    /// <summary>
    /// Creates a failed operation result with a message
    /// </summary>
    public static new OperationResult<T> Failure(string message) => new() { IsSuccess = false, Message = message };

    /// <summary>
    /// Implicit conversion from the data type to a successful operation result
    /// </summary>
    public static implicit operator OperationResult<T>(T data) => Success(data);
}
