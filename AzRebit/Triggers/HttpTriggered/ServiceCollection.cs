using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzRebit.Shared;
using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Middleware;

using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Triggers.HttpTriggered;

internal class ServiceCollection:IFeatureSetup
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ITriggerHandler, HttpResubmitHandler>();
        services.AddSingleton<IMiddlewareHandler, HttpMiddlewareHandler>();
    }
}
