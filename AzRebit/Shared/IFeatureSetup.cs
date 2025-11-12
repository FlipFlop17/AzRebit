using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Shared;

/// <summary>
/// Used to mark features that need to register services during startup
/// </summary>
internal interface IFeatureSetup
{
    Type TriggerAttribute { get; }
    static abstract void RegisterServices(IServiceCollection services);

     object CreateTriggerMetadata(ParameterInfo parameter);
}
