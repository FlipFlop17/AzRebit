using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Triggers.HttpTriggered;
internal class HttpFeatureSetup : IFeatureSetup
{
    public TriggerTypes.TriggerType TriggerSupport => TriggerTypes.TriggerType.Http;

    public Type TriggerAttribute => typeof(HttpTriggerAttribute);

    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ITriggerHandler, HttpResubmitHandler>();
        services.AddSingleton<IMiddlewareHandler, HttpMiddlewareHandler>();
    }

    public object? CreateTriggerMetadata(ParameterInfo parameter)
    {
        //http requests don't need extra metadata as all info is in the incoming http request which is already saved
        return null;
    }
}
