using System.Text;

using AzRebit.Infrastructure;
using AzRebit.Model;
using AzRebit.Shared;

using Azure.Storage.Queues;

using Microsoft.Extensions.Azure;

namespace AzRebit.Triggers.QueueTrigger.ResubmitHandler;

internal class StorageQueueResubmitHandler : IResubmitHandler
{
    private readonly IResubmitStorage _blobStorage;
    private readonly IAzureClientFactory<QueueServiceClient> _queueServiceClientFactory;

    public TriggerTypes.TriggerName HandlerType => TriggerTypes.TriggerName.Queue;
    public StorageQueueResubmitHandler(IResubmitStorage blobStorage,IAzureClientFactory<QueueServiceClient> queueClient)
    {
        _blobStorage = blobStorage;
        _queueServiceClientFactory = queueClient;
    }
    public async Task<RebitActionResult> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        var storedMessage = await _blobStorage.FindAsync(invocationId);
        if (storedMessage is null)
            return RebitActionResult.Failure("Queue message not found");
        
        var msg=await storedMessage.DownloadContentAsync(); //since queue messages are small we can download them eniterly

        var resubmitPayload= msg.Value.Content.ToString();
        function.TriggerMetadata.TryGetValue("QueueName", out string? inputQueueName);
        QueueClient destinationQueue = CreateQueueClient(function.Name,inputQueueName!);

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
