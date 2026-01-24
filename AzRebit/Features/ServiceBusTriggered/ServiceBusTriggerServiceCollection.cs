using AzRebit.Domain.Abstractions;
using AzRebit.Features.ServiceBusTriggered.ResubmitHandler;
using AzRebit.Features.ServiceBusTriggered.SaveRequestMiddleware;
using AzRebit.Infrastructure.FileStorage;

using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.ServiceBusTriggered;

internal class ServiceBusTriggerServiceCollection : ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IResubmitHandler>(sp =>
            new ServiceBusResubmitHandler(
                sp.GetRequiredService<IResubmitStorage>(),
                sp.GetRequiredService<IAzureClientFactory<ServiceBusClient>>(),
                sp.GetRequiredService<ILogger<ServiceBusResubmitHandler>>()
            ));
        services.AddSingleton<ISavePayloadHandler>(sp =>
            new ServiceBusMiddlewareHandler(
                sp.GetRequiredService<ILogger<ServiceBusMiddlewareHandler>>(),
                sp.GetRequiredService<IAzureClientFactory<BlobServiceClient>>(),
                sp.GetRequiredService<IResubmitStorage>()
            ));
    }
}