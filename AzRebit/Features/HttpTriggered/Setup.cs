using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.DependencyInjection;



namespace AzRebit.Triggers.HttpTriggered;

/// <summary>
/// Setup is needed for the assembly discovery process to find and register this feature
/// </summary>
internal class Setup : TriggerSetupBase
{
    public override TriggerType TriggerName => TriggerType.Http;
    public override Type TriggerAttribute => typeof(HttpTriggerAttribute);
    public override AzFunction TryCreateAzFunction(string functionName, TriggerBindingAttribute triggerAttribute, IServiceCollection services)
    {
        try
        {
            if (triggerAttribute is not HttpTriggerAttribute httpAtribute) throw new ArgumentNullException();

            return new AzFunction(functionName, TriggerName);
        }
        catch (Exception e)
        {
            throw new AzFunctionNotCreatedException(e.Message,e);
        }

    }

}
