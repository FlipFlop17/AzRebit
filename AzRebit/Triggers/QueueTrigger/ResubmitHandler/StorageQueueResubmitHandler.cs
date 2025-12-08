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
    private readonly IAzureClientFactory<QueueServiceClient> _queueServiceClientFactory;

    public TriggerTypes.TriggerName HandlerType => TriggerTypes.TriggerName.Queue;
    public StorageQueueResubmitHandler(IAzureClientFactory<BlobServiceClient> blobClientFactory,IAzureClientFactory<QueueServiceClient> queueClient)
    {
        _blobResubmitContainer = blobClientFactory
            .CreateClient(QueueMiddlewareHandler.ResubmitContainerNameName)
            .GetBlobContainerClient(QueueMiddlewareHandler.ResubmitContainerNameName);
        _queueServiceClientFactory = queueClient;
    }
    public async Task<RebitActionResult> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        //2. fetch the stored message
        var storedBlobName = $"{IMiddlewareHandler.BlobPrefixForQueue}{invocationId}.txt";
        var storedMessage = await _blobResubmitContainer
            .GetBlobClient(storedBlobName)
            .DownloadContentAsync(); //since queue messages are small we can download them eniterly

        var resubmitPayload= storedMessage.Value.Content.ToString();
        function.TriggerMetadata.TryGetValue("QueueName", out string inputQueueName);
        QueueClient destinationQueue = CreateQueueClient(function.Name,inputQueueName);

        var msgResult=await destinationQueue.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(resubmitPayload)));
        
        if (msgResult.Value.MessageId is not null) { 
            var msgToReturn = msgResult.Value.ToString() ?? "Message added to the queue (resubmited)";
            return RebitActionResult.Success(msgToReturn);
        }

        return RebitActionResult.Failure("Failed to add message to the queue");
    }

    private QueueClient CreateQueueClient(string functionName,string queueName)
    {
        return _queueServiceClientFactory.CreateClient(functionName).GetQueueClient(queueName);
    }
}
