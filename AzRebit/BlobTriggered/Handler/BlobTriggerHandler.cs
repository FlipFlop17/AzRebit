using AzRebit.BlobTriggered.Model;
using AzRebit.Extensions;
using AzRebit.Shared;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.BlobTriggered.Handler;
internal class BlobTriggerHandler : ITriggerHandler

{
    /// <summary>
    /// The name of the tag that is used to mark the blob file
    /// </summary>
    public const string BlobInputTagName = "input-InvocationId";
    /// <summary>
    /// the name of the container where blobs for resubmition are stored
    /// </summary>
    public const string BlobResubmitContainerName = "blob-resubmits";
    public string ContainerName => BlobResubmitContainerName;
    public TriggerType HandlerType => TriggerType.Blob;
    public async Task HandleResubmitAsync<T>(T triggerDetails,string invocationId)
    {
        if (triggerDetails is not BlobTriggerDetails blobDetails)
        {
            throw new ArgumentException($"Expected {nameof(BlobTriggerDetails)} but received {triggerDetails?.GetType().Name}", nameof(triggerDetails));
        }
        IDictionary<string, string> tags = new Dictionary<string, string>();

        BlobContainerClient resubmitContainerClient = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), BlobResubmitContainerName);

        BlobClient? blobForResubmitClient = await resubmitContainerClient.PickUpBlobForResubmition(invocationId);
        var existingTagsResponse = await blobForResubmitClient.GetTagsAsync();
        //upload the blob to the azure function trigger container which will trigger the logic app
        BlobContainerClient inputContainer = new BlobContainerClient(blobDetails.ConnectionString, blobDetails.ContainerName);
        AppendBlobClient inputBlob= inputContainer.GetAppendBlobClient(blobForResubmitClient.GetBlobName());
        await inputBlob.StartCopyFromUriAsync(blobForResubmitClient.Uri);

        if (existingTagsResponse.Value is not null)
        {
            await inputBlob.SetTagsAsync(existingTagsResponse.Value.Tags);
        }
    }
}
