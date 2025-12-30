using AzRebit.Domain.Abstractions;
using AzRebit.Features.QueueTriggered.ResubmitHandler;
using AzRebit.Features.QueueTriggered.SaveRequestMiddleware;

using Microsoft.Extensions.DependencyInjection;

internal class QueueTriggerServiceCollection : ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IResubmitHandler, StorageQueueResubmitHandler>();
        services.AddSingleton<ISavePayloadHandler, QueueMiddlewareHandler>();
    }
}