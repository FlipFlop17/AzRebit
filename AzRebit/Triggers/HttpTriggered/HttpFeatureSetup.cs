using System.Reflection;

using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Middleware;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.HttpTriggered;

/// <summary>
/// Setup is needed for the assembly discovery process to find and register this feature
/// </summary>
internal class HttpFeatureSetup : ITriggerSetup
{
    public TriggerTypes.TriggerType TriggerSupport => TriggerTypes.TriggerType.Http;

    public Type TriggerAttribute => typeof(HttpTriggerAttribute);

    public static AzFunction CreateAzFunction(string functionName, ParameterInfo parameter, IServiceCollection services)
    {
        var functionMeta = new Dictionary<string,string>();
        return new AzFunction(functionName, TriggerType.Http, functionMeta);
    }

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
}
