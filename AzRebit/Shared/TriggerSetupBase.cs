using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using AzRebit.Shared.Model;

using Microsoft.Extensions.DependencyInjection;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Shared;

internal abstract class TriggerSetupBase
{
    public abstract TriggerType TriggerSupport { get; }

    /// <summary>
    /// Tries to create a ne AzFunction object based on the trigger params and Function implementation atributes
    /// </summary>
    /// <param name="functionName"></param>
    /// <param name="parameter"></param>
    /// <param name="services"></param>
    /// <returns>AzFunction</returns>
    public abstract AzFunction TryCreateAzFunction(string functionName, ParameterInfo[] parameter, IServiceCollection services);

}
