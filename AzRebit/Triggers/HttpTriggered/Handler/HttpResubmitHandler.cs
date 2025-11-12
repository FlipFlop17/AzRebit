using AzRebit.HelperExtensions;
using AzRebit.Shared;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.HttpTriggered.Handler;

internal class HttpResubmitHandler:ITriggerHandler
{
    /// <summary>
    /// The name of the tag that is used to mark the blob file
    /// </summary>
    public const string HttpInputTagName = "input-InvocationId";
    /// <summary>
    /// Container name where the http requests are saved for resubmiting
    /// </summary>
    public const string HttpResubmitContainerName = "http-resubmits";
    public TriggerType HandlerType => TriggerType.Http;
    public Type TriggerAttribute => typeof(HttpTriggerAttribute);
    public string ContainerName => HttpResubmitContainerName;
    private HttpTriggerAttribute _triggerAttributeMetadata { get; set; }

    public async Task<bool> HandleResubmitAsync(string invocationId)
    {
        BlobContainerClient httpContainer = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), HttpResubmitContainerName);
        var blobForResubmit = await httpContainer.PickUpBlobForResubmition(invocationId);

        if (blobForResubmit is not null)
        {
            var downloadResponse = await blobForResubmit.DownloadAsync();
            using var streamReader = new StreamReader(downloadResponse.Value.Content);
            var httpRequestContent = await streamReader.ReadToEndAsync();
        }

        //TODO: Implement the actual resubmit logic here, the POST request to the function endpoint with the saved request data

        return true;
    }
}
