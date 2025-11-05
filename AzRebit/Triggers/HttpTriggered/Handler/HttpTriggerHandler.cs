using AzRebit.Extensions;
using AzRebit.Shared;

using Azure.Storage.Blobs;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.HttpTriggered.Handler;

internal class HttpTriggerHandler:ITriggerHandler
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
    public string ContainerName => HttpResubmitContainerName;
    public async Task HandleResubmitAsync<T>(T triggerDetails,string invocationId)
    {

        BlobContainerClient httpContainer = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), HttpResubmitContainerName);
        var blobForResubmit = await httpContainer.PickUpBlobForResubmition(invocationId);
        if (blobForResubmit is not null)
        {
            var downloadResponse = await blobForResubmit.DownloadAsync();
            using var streamReader = new StreamReader(downloadResponse.Value.Content);
            var httpRequestContent = await streamReader.ReadToEndAsync();
        }
    }
}
