using AzRebit.Domain.Abstractions;
using AzRebit.Features.HttpTriggered.ResubmitHandler;
using AzRebit.Features.HttpTriggered.SaveRequestMiddleware;

using Microsoft.Extensions.DependencyInjection;

internal class HttpTriggerServiceCollection : ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IResubmitHandler, HttpResubmitHandler>();
        services.AddSingleton<ISavePayloadHandler, HttpMiddlewareHandler>();
    }
}