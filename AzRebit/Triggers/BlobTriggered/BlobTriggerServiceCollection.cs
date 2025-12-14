using AzRebit.Shared;
using AzRebit.Triggers.BlobTriggered.Handler;
using AzRebit.Triggers.BlobTriggered.Middleware;

using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Triggers.BlobTriggered;

internal class BlobTriggerServiceCollection: ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IResubmitHandler, BlobResubmitHandler>();
        services.AddSingleton <ISavePayloadHandler, BlobMiddlewareHandler>();
    }
}
