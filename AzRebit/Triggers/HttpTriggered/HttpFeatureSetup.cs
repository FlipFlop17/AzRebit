using System.Reflection;

using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Middleware;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Triggers.HttpTriggered;

/// <summary>
/// Setup is needed for the assembly discovery process to find and register this feature
/// </summary>
internal class HttpFeatureSetup : IFeatureSetup
{
    public TriggerTypes.TriggerType TriggerSupport => TriggerTypes.TriggerType.Http;

    public Type TriggerAttribute => typeof(HttpTriggerAttribute);

    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IResubmitHandler, HttpResubmitHandler>();
        services.AddSingleton<IMiddlewareHandler, HttpMiddlewareHandler>();
        services.AddAzureClients(clients =>
        {
            clients.AddBlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!)
            .WithName(HttpMiddlewareHandler.HttpResubmitContainerName);
        });
    }

    public object? CreateTriggerMetadata(ParameterInfo parameter)
    {
        //http requests don't need extra metadata as all info is in the incoming http request which is already saved
        return null;
    }
}
