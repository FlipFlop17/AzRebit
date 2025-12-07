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
    public abstract Type TriggerAttribute { get; }
    public abstract AzFunction CreateAzFunction(string functionName, ParameterInfo parameter, IServiceCollection services);

}
