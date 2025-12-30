using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Domain.Results;


namespace AzRebit.Domain.Abstractions;

internal interface IResubmitHandler
{
    internal TriggerType HandlerType { get; }
    /// <summary>
    /// Attempts to resubmit a previously triggered invocation using the specified invocation identifier and trigger
    /// attribute metadata.
    /// </summary>
    /// <param name="invocationId">The unique identifier of the invocation to be resubmitted. Cannot be null or empty.</param>
    /// <param name="function"></param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the resubmission
    /// was successful; otherwise, <see langword="false"/>.</returns>
    internal Task<RebitActionResult<ResubmitHandlerResponse>> HandleResubmitAsync(string invocationId, AzFunction function);
}

internal record ResubmitHandlerResponse(string? ResubmitingFileName);
