using AzRebit.Domain.Abstractions;
using AzRebit.Features.ServiceBusTriggered.ResubmitHandler;
using AzRebit.Features.ServiceBusTriggered.SaveRequestMiddleware;
using AzRebit.Infrastructure.FileStorage;

using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

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
                sp.GetRequiredService<ILogger<ServiceBusResubmitHandler>>(),
                sp.GetRequiredService<IAzureClientFactory<ServiceBusClient>>(),
                sp.GetRequiredService<IResubmitStorage>()
            ));
        
        services.AddSingleton<ISavePayloadHandler>(sp =>
            new ServiceBusMiddlewareHandler(
                sp.GetRequiredService<ILogger<ServiceBusMiddlewareHandler>>(),
                sp.GetRequiredService<IResubmitStorage>(),
                sp.GetRequiredService<ServiceBusClient>(),
                sp.GetRequiredService<ServiceBusAdministrationClient>()
            ));
    }
}