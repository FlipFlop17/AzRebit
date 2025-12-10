using Azure.Storage.Queues;

using Microsoft.Extensions.Azure;

namespace AzRebit.FunctionExample.Infra;

internal class QueueStorage:IFunctionOutput
{
    private QueueServiceClient queueService;
    private QueueClient queue;

    public QueueStorage(IAzureClientFactory<QueueServiceClient> queueFactory)
    {
        queueService = queueFactory.CreateClient("function-output-queue");
        queue = queueService.GetQueueClient("function-output");
    }
    public async Task PostOutputAsync(string message)
    {
        await queue.SendMessageAsync(message);
    }
}
