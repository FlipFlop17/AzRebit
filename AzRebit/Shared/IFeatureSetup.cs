using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Shared;

/// <summary>
/// Used to mark features that need to register services during startup
/// </summary>
internal interface IFeatureSetup
{
    TriggerType TriggerSupport { get;}
    Type TriggerAttribute { get; }
    static abstract void RegisterServices(IServiceCollection services);

     object? CreateTriggerMetadata(ParameterInfo parameter);
}
