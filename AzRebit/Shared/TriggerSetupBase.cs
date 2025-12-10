using System.Reflection;

using AzRebit.Model;

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.DependencyInjection;

using static AzRebit.Model.TriggerTypes;

namespace AzRebit.Shared;

internal abstract class TriggerSetupBase
{
    public abstract TriggerName TriggerName { get; }
    public abstract Type TriggerAttribute { get; }

    /// <summary>
    /// Tries to create a ne AzFunction object based on the trigger params and Function implementation atributes
    /// </summary>
    /// <param name="functionName"></param>
    /// <param name="parameter"></param>
    /// <param name="services"></param>
    /// <returns>AzFunction</returns>
    public abstract AzFunction TryCreateAzFunction(string functionName, TriggerBindingAttribute triggerAtribute, IServiceCollection services);
    

}
