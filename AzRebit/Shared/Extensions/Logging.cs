using Microsoft.Extensions.Logging;

namespace AzRebit.Shared.Extensions;

internal static partial class AzRebitLoggerExtension
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Start saving incoming work data for invocationId: {InvocationId} functionName: {FunctionName}")]
    public static partial void LogMiddlewareProcessing(this ILogger logger, string invocationId, string functionName);

    [LoggerMessage(
       EventId = 1002,
       Level = LogLevel.Information,
       Message = "Finished saving incoming work data for invocationId: {InvocationId}, functionName: {FunctionName}, isSuccess: {IsSuccess}, msg: {Message}")]
    public static partial void LogMiddlewareFinished(this ILogger logger, string invocationId, string functionName, bool isSuccess, string? message);

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Error,
        Message = "Unexpected error while saving incoming work data: {InvocationId} functionName: {FunctionName}")]
    public static partial void LogMiddlewareError(this ILogger logger, Exception ex, string invocationId, string functionName);


    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Error,
        Message = "Unexpected error while saving incoming work data: {InvocationId} functionName: {FunctionName}")]
    public static partial void LogBlobOperationError(this ILogger logger, Exception ex, string invocationId, string functionName);

    [LoggerMessage(
       EventId = 1003,
       Level = LogLevel.Information,
       Message = "Resubmit requested for invocationId: {InvocationId}, functionName: {FunctionName}")]
    public static partial void LogResubmitStart(this ILogger logger, string invocationId, string functionName);

    [LoggerMessage(
       EventId = 1004,
       Level = LogLevel.Information,
       Message = "Resubmit status for invocationId: {InvocationId}, functionName: {FunctionName}, isSuccess: {IsSuccess}, msg:{Message}")]
    public static partial void LogResubmitStatus(this ILogger logger, string invocationId, string functionName, bool isSuccess, string? message);

    [LoggerMessage(
       EventId = 5003,
       Level = LogLevel.Error,
       Message = "Resubmit failed for invocationId: {InvocationId}, functionName: {FunctionName}")]
    public static partial void LogResubmitError(this ILogger logger, Exception ex, string invocationId, string functionName);

    [LoggerMessage(
      EventId = 5004,
      Level = LogLevel.Error,
      Message = "Resubmit validation error {ValidationMessage}")]
    public static partial void LogValidationError(this ILogger logger, string validationMessage);

    [LoggerMessage(
       EventId = 1005,
       Level = LogLevel.Information,
       Message = "Resubmiting work data: {InvocationId}, functionName: {FunctionName}, fileName: {FileName}")]
    public static partial void LogResubmitWorkData(this ILogger logger, string invocationId, string functionName, string fileName);



}
