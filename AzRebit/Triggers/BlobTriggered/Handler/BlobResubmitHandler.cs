using AzRebit.HelperExtensions;
using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.BlobTriggered.Middleware;
using AzRebit.Triggers.BlobTriggered.Model;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.BlobTriggered.Handler;


/// <summary>
/// The handler for blob triggered resubmissions. Does the work of copying the blob from the resubmit container to the input container
/// </summary>
internal class BlobResubmitHandler : ITriggerHandler

{

    public TriggerType HandlerType => TriggerType.Blob;

    public async Task<ActionResult> HandleResubmitAsync(string invocationId, object? triggerAttributeMetadata)
    {
        BlobTriggerAttributeMetadata blobTriggerAttributeMetadata = (BlobTriggerAttributeMetadata)triggerAttributeMetadata!;
        IDictionary<string, string> tags = new Dictionary<string, string>();

        BlobContainerClient resubmitContainerClient = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), BlobMiddlewareHandler.BlobResubmitContainerName);

        BlobClient? blobForResubmitClient = await resubmitContainerClient.PickUpBlobForResubmition(invocationId);
        if (blobForResubmitClient is null)
        {
            return ActionResult.Failure($"No blob found for invocation id {invocationId} in container {blobTriggerAttributeMetadata.ContainerName}");
        }
        var existingTagsResponse = await blobForResubmitClient.GetTagsAsync();
        //upload the blob to the azure function trigger container which will trigger the logic app
        //TODO: trebas skuziti kako spremiti naziv blob input containera za blob. 
        //Blob trigger moze imati Connection parametar koji referencira Environment varijablu ali i ne mora imati, ako nema onda se koristi default connection string - tj. storage account od funkcije. Takoder mozes imati i [StorageAccount] atribut na klasi. to mozda kasnije ubaci kao feature
        BlobContainerClient inputContainer = new BlobContainerClient(blobTriggerAttributeMetadata.Connection, blobTriggerAttributeMetadata.ContainerName);
        AppendBlobClient inputBlob= inputContainer.GetAppendBlobClient(blobForResubmitClient.GetBlobName());
        await inputBlob.StartCopyFromUriAsync(blobForResubmitClient.Uri);

        if (existingTagsResponse.Value is not null)
        {
            await inputBlob.SetTagsAsync(existingTagsResponse.Value.Tags);
        }
        return ActionResult.Success();
    }

  


}
