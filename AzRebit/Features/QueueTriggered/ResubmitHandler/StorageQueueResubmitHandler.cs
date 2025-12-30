using System.Text;

using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;

using Azure.Storage.Queues;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.QueueTriggered.ResubmitHandler;

internal class StorageQueueResubmitHandler : IResubmitHandler
{
    private readonly IResubmitStorage _blobStorage;
    private readonly IAzureClientFactory<QueueServiceClient> _queueServiceClientFactory;
    private readonly ILogger<StorageQueueResubmitHandler> _logger;

    public TriggerType HandlerType => TriggerType.Queue;
    public StorageQueueResubmitHandler(IResubmitStorage blobStorage, IAzureClientFactory<QueueServiceClient> queueClient, ILogger<StorageQueueResubmitHandler> logger)
    {
        _blobStorage = blobStorage;
        _queueServiceClientFactory = queueClient;
        _logger = logger;
    }
    public async Task<RebitActionResult<ResubmitHandlerResponse>> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        try
        {
            var storedMessage = await _blobStorage.FindAsync(invocationId);
            if (storedMessage is null)
                return RebitActionResult<ResubmitHandlerResponse>.Failure("Queue message not found");

            var msg = await storedMessage.DownloadContentAsync(); //since queue messages are small we can download them eniterly

            var resubmitPayload = msg.Value.Content.ToString();
            var inputQueueName = function.GetFunctionTriggerQueueName();
            if (string.IsNullOrEmpty(inputQueueName))
                return RebitActionResult<ResubmitHandlerResponse>.Failure("Input queue name not found");

            QueueClient destinationQueue = CreateQueueClient(function.Name, inputQueueName!);

            var msgResult = await destinationQueue.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(resubmitPayload)));

            if (msgResult.Value.MessageId is not null)
            {
                var msgToReturn = msgResult.Value.ToString() ?? "Message added to the queue (resubmited)";
                return RebitActionResult<ResubmitHandlerResponse>.Success(new ResubmitHandlerResponse(storedMessage.Name), msgToReturn);
            }

            return RebitActionResult<ResubmitHandlerResponse>.Failure("Failed to add message to the queue");
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Unexpected error while resubmiting QueueMessage");
            return RebitActionResult<ResubmitHandlerResponse>.Failure(e.Message);
        }
    }

    private QueueClient CreateQueueClient(string functionName, string queueName)
    {
        return _queueServiceClientFactory.CreateClient(functionName).GetQueueClient(queueName);
    }
}
