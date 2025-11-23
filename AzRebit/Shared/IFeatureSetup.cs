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

    
    /// <summary>
    /// Factory for creating trigger metadata objects
    /// </summary>
    /// <remarks>
    /// Usually you should create a new class that holds the important metadata you might need later.
    /// </remarks>
    /// <param name="parameter"></param>
    /// <returns></returns>
    object? CreateTriggerMetadata(ParameterInfo parameter);
}
