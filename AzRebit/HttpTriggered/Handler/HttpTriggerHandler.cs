using AzRebit.Extensions;
using AzRebit.Shared;

using Azure.Storage.Blobs;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.HttpTriggered.Handler;

internal class HttpTriggerHandler:ITriggerHandler
{
    public TriggerType HandlerType => TriggerType.Http;

    public async Task HandleResubmitAsync<T>(T triggerDetails,string invocationId)
    {

        BlobContainerClient httpContainer = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), HttpExtensions.HttpResubmitContainerName);
        var blobForResubmit = await httpContainer.PickUpBlobForResubmition(invocationId);
        if (blobForResubmit is not null)
        {
            var downloadResponse = await blobForResubmit.DownloadAsync();
            using var streamReader = new StreamReader(downloadResponse.Value.Content);
            var httpRequestContent = await streamReader.ReadToEndAsync();
        }
    }
}
