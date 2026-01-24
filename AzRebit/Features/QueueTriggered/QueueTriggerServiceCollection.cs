using AzRebit.Domain.Abstractions;
using AzRebit.Features.QueueTriggered.ResubmitHandler;
using AzRebit.Features.QueueTriggered.SaveRequestMiddleware;
using AzRebit.Infrastructure.FileStorage;

using Azure.Storage.Blobs;
using Azure.Storage.Queues;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal class QueueTriggerServiceCollection : ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IResubmitHandler>(sp =>
            new StorageQueueResubmitHandler(
                sp.GetRequiredService<IResubmitStorage>(),
                sp.GetRequiredService<IAzureClientFactory<QueueServiceClient>>(),
                sp.GetRequiredService<ILogger<StorageQueueResubmitHandler>>()
            ));
        services.AddSingleton<ISavePayloadHandler>(sp =>
            new QueueMiddlewareHandler(
                sp.GetRequiredService<ILogger<QueueMiddlewareHandler>>(),
                sp.GetRequiredService<IAzureClientFactory<BlobServiceClient>>(),
                sp.GetRequiredService<IResubmitStorage>()
            ));
    }
}