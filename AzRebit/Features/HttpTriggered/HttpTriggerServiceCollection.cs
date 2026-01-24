using AzRebit.Domain.Abstractions;
using AzRebit.Features.HttpTriggered.ResubmitHandler;
using AzRebit.Features.HttpTriggered.SaveRequestMiddleware;
using AzRebit.Infrastructure.FileStorage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal class HttpTriggerServiceCollection : ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IResubmitHandler>(sp =>
            new HttpResubmitHandler(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IResubmitStorage>(),
                sp.GetRequiredService<ILogger<HttpResubmitHandler>>()
            ));
        services.AddSingleton<ISavePayloadHandler>(sp =>
            new HttpMiddlewareHandler(
                sp.GetRequiredService<ILogger<HttpMiddlewareHandler>>(),
                sp.GetRequiredService<IResubmitStorage>()
            ));
    }
}