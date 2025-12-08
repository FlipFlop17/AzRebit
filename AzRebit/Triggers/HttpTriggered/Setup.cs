using AzRebit.Shared;
using AzRebit.Shared.Model;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.DependencyInjection;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.HttpTriggered;

/// <summary>
/// Setup is needed for the assembly discovery process to find and register this feature
/// </summary>
internal class Setup : TriggerSetupBase
{
    public override TriggerName TriggerName => TriggerName.Blob;
    public override Type TriggerAttribute => typeof(HttpTriggerAttribute);
    public override AzFunction TryCreateAzFunction(string functionName, TriggerBindingAttribute triggerAttribute, IServiceCollection services)
    {
        try
        {
            if (triggerAttribute is not HttpTriggerAttribute httpAtribute) throw new ArgumentNullException();
            var functionMeta = new Dictionary<string, string>();

            return new AzFunction(functionName, TriggerName, functionMeta);
        }
        catch (Exception e)
        {
            throw new AzFunctionNotCreatedException(e.Message,e);
        }

    }

}
