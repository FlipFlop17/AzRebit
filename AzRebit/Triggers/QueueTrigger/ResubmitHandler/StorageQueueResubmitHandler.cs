using System.Text;

using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.QueueTrigger.SaveRequestMiddleware;

using Azure.Storage.Blobs;
using Azure.Storage.Queues;

using Microsoft.Extensions.Azure;

namespace AzRebit.Triggers.QueueTrigger.ResubmitHandler;

internal class StorageQueueResubmitHandler : IResubmitHandler
{
    private BlobContainerClient _blobResubmitContainer;

    public TriggerTypes.TriggerType HandlerType => TriggerTypes.TriggerType.Queue;
    public StorageQueueResubmitHandler(IAzureClientFactory<BlobServiceClient> blobClientFactory)
    {
        _blobResubmitContainer = blobClientFactory
            .CreateClient(QueueMiddlewareHandler.ResubmitContainerNameName)
            .GetBlobContainerClient(QueueMiddlewareHandler.ResubmitContainerNameName);
    }
    public async Task<RebitActionResult> HandleResubmitAsync(string invocationId, object? triggerAttributeMetadata)
    {
        if (triggerAttributeMetadata is not QueueTriggerAttributeMetadata queueTriggerData)
            return RebitActionResult.Failure("Trigger attribute data is missing");

        //1. Parse triggerAttributeMetadata to get queue name
        var queueName = queueTriggerData.QueueName;

        //2. fetch the stored message
        var storedBlobName = $"{IMiddlewareHandler.BlobPrefixForQueue}{invocationId}.txt";
        var storedMessage = await _blobResubmitContainer
            .GetBlobClient(storedBlobName)
            .DownloadContentAsync(); //since queue messages are small we can download them eniterly

        var resubmitPayload= storedMessage.Value.Content.ToString();
        QueueClient destinationQueue=new QueueClient(queueTriggerData.ConnectionString, queueTriggerData.QueueName);
        
        var msgResult=await destinationQueue.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(resubmitPayload)));
        
        if (msgResult.Value.MessageId is not null) { 
            var msgToReturn = msgResult.Value.ToString() ?? "Message added to the queue (resubmited)";
            return RebitActionResult.Success(msgToReturn);
        }

        return RebitActionResult.Failure("Failed to add message to the queue");
    }
}
