using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Shared.Model;

/// <summary>
/// Non-generic version for operations without data payload
/// </summary>
public class RebitActionResult 
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }

    public static RebitActionResult Success() => new() { IsSuccess = true };

    public static RebitActionResult Success(string message) => new()
    {
        IsSuccess = true,
        Message = message
    };

    public static RebitActionResult Failure() => new() { IsSuccess = false };

    public static RebitActionResult Failure(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };
}
/// <summary>
/// Represents the result of an operation, optionally with a data payload
/// </summary>
/// <typeparam name="T">The type of the data payload (use object for no specific data)</typeparam>
public class RebitActionResult<T>: RebitActionResult
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
    /// The data payload of the operation
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful operation result with data
    /// </summary>
    public static RebitActionResult<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a successful operation result with data and message
    /// </summary>
    public static RebitActionResult<T> Success(T data, string message) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message
    };

    /// <summary>
    /// Creates a successful operation result without data
    /// </summary>
    public static RebitActionResult<T> Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a successful operation result without data but with message
    /// </summary>
    public static RebitActionResult<T> Success(string message) => new()
    {
        IsSuccess = true,
        Message = message
    };

    /// <summary>
    /// Creates a failed operation result
    /// </summary>
    public static RebitActionResult<T> Failure() => new() { IsSuccess = false };

    /// <summary>
    /// Creates a failed operation result with a message
    /// </summary>
    public static RebitActionResult<T> Failure(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };

    /// <summary>
    /// Implicit conversion from the data type to a successful operation result
    /// </summary>
    public static implicit operator RebitActionResult<T>(T data) => Success(data);
}

