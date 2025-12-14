using AzRebit.Shared;
using AzRebit.Triggers.QueueTrigger.ResubmitHandler;
using AzRebit.Triggers.QueueTrigger.SaveRequestMiddleware;

using Microsoft.Extensions.DependencyInjection;

internal class QueueTriggerServiceCollection: ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IResubmitHandler, StorageQueueResubmitHandler>();
        services.AddSingleton<ISavePayloadHandler, QueueMiddlewareHandler>();
    }
}