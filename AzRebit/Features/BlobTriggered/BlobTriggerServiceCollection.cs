using AzRebit.Domain.Abstractions;
using AzRebit.Features.BlobTriggered.ResubmitHandler;
using AzRebit.Features.BlobTriggered.SaveRequestMiddleware;

using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Features.BlobTriggered;

internal class BlobTriggerServiceCollection : ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IResubmitHandler, BlobResubmitHandler>();
        services.AddSingleton<ISavePayloadHandler, BlobMiddlewareHandler>();
    }
}
