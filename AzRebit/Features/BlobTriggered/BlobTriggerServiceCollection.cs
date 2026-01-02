using AzRebit.Domain.Abstractions;
using AzRebit.Features.BlobTriggered.ResubmitHandler;
using AzRebit.Features.BlobTriggered.SaveRequestMiddleware;
using AzRebit.Infrastructure.FileStorage;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.BlobTriggered;

internal class BlobTriggerServiceCollection : ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IResubmitHandler>(sp =>
            new BlobResubmitHandler(
                sp.GetRequiredService<ILogger<BlobResubmitHandler>>(),
                sp.GetRequiredService<IAzureClientFactory<BlobServiceClient>>(),
                sp.GetRequiredService<IResubmitStorage>()
            ));
        services.AddSingleton<ISavePayloadHandler>(sp =>
            new BlobMiddlewareHandler(
                sp.GetRequiredService<ILogger<BlobMiddlewareHandler>>(),
                sp.GetRequiredService<IResubmitStorage>()
            ));
    }
}
