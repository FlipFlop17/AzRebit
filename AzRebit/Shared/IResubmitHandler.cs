using AzRebit.Model;
using AzRebit.Triggers.BlobTriggered.Model;

using static AzRebit.Model.TriggerTypes;

namespace AzRebit.Shared;

internal interface IResubmitHandler
{
    public TriggerName HandlerType { get; }
    /// <summary>
    /// Attempts to resubmit a previously triggered invocation using the specified invocation identifier and trigger
    /// attribute metadata.
    /// </summary>
    /// <param name="invocationId">The unique identifier of the invocation to be resubmitted. Cannot be null or empty.</param>
    /// <param name="triggerAttributeMetadata">The metadata associated with the trigger attribute for the invocation. Can be null since some triggers like HTTP are dynaamic</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the resubmission
    /// was successful; otherwise, <see langword="false"/>.</returns>
    public Task<RebitActionResult> HandleResubmitAsync(string invocationId, AzFunction function);
}
