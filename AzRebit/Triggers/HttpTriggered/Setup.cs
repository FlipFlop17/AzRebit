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
internal class Setup : TriggerSetupBase
{
    public override TriggerType TriggerSupport => TriggerType.Blob;

    public override AzFunction TryCreateAzFunction(string functionName, ParameterInfo[] parameters, IServiceCollection services)
    {
        HttpTriggerAttribute? triggerAtr;
        var functionMeta = new Dictionary<string, string>();
        try
        {
            var functionsTriggerParameter = parameters.FirstOrDefault(
                            p => p.GetCustomAttribute<HttpTriggerAttribute>() != null
                        );

            if (functionsTriggerParameter is null)
                throw new ArgumentNullException(nameof(functionsTriggerParameter));

            triggerAtr = functionsTriggerParameter.GetCustomAttribute<HttpTriggerAttribute>();

            if (triggerAtr is null)
                throw new ArgumentNullException(nameof(triggerAtr));

            return new AzFunction(functionName, TriggerType.Http, functionMeta);
        }
        catch (ArgumentNullException e)
        {
            throw new AzFunctionNotCreatedException(e.Message, e);
        }
        catch(Exception ex)
        {
            throw new AzFunctionNotCreatedException(ex.Message, ex);
        }

    }


}
