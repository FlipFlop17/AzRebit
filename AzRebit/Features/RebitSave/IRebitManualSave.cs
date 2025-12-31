using System.Text;

using AzRebit.Domain.Results;

namespace AzRebit.Features.RebitSave;

/// <summary>
/// Defines methods for saving payloads for later resubmission, associating them with a function name and optional file
/// metadata.
/// </summary>
/// <remarks>Implementations of this interface allow clients to persist payload data—either as a string or a
/// stream—for deferred processing or retry scenarios. The saved payload can be tagged with metadata and stored using a
/// specified encoding. This interface is intended for scenarios where reliable, manual resubmission of data is
/// required.</remarks>
public interface IRebitManualSave
{
    /// <summary>
    /// Saves the specified payload for later resubmission, associating it with a function name and optional file
    /// metadata.
    /// </summary>
    /// <param name="payload">The payload data to be saved for resubmission. Cannot be null or empty.</param>
    /// <param name="functionName">The name of the function with which the payload is associated. Cannot be null or empty.</param>
    /// <param name="id"> The unique id of the run. Usually fetched from context.InvocationId</param>
    /// <param name="fileName">The name of the file to use when saving the payload. If not specified, a default name is used.</param>
    /// <param name="destinationFileTags">An optional collection of key-value pairs to tag the saved file. Can be null if no tags are required.</param>
    /// <param name="encoding">The text encoding to use when saving the payload. If null, the default encoding is used.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains a RebitActionResult indicating
    /// the outcome of the operation.</returns>
    Task<RebitActionResult> SavePayloadForResubmit(string payload, string functionName, string id,string? fileName = default, IDictionary<string, string>? destinationFileTags = default, Encoding? encoding = default);

    /// <summary>
    /// Saves the specified payload for later resubmission, associating it with a function name and optional file
    /// metadata.
    /// </summary>
    /// <param name="payload">The payload data to be saved for resubmission. Cannot be null or empty.</param>
    /// <param name="functionName">The name of the function with which the payload is associated. Cannot be null or empty.</param>
    /// <param name="id"> The unique id of the run. Usually fetched from context.InvocationId</param>
    /// <param name="fileName">The name of the file to use when saving the payload. If not specified, a default name is used.</param>
    /// <param name="destinationFileTags">An optional collection of key-value pairs to tag the saved file. Can be null if no tags are required.</param>
    /// <param name="encoding">The text encoding to use when saving the payload. If null, the default encoding is used.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains a RebitActionResult indicating
    /// the outcome of the operation.</returns>
    Task<RebitActionResult> SavePayloadForResubmit(Stream payload, string functionName,string id, string? fileName = default, IDictionary<string, string>? destinationFileTags = default, Encoding? encoding = default);
}
