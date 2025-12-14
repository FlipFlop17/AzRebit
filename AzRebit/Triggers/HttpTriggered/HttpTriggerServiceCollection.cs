using AzRebit.Shared;
using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Middleware;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

internal class HttpTriggerServiceCollection:ITriggersServiceCollection
{
     public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IResubmitHandler, HttpResubmitHandler>();
        services.AddSingleton<ISavePayloadHandler, HttpMiddlewareHandler>();
    }
}