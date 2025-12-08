using AzRebit.Shared;
using AzRebit.Shared.Model;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.DependencyInjection;


namespace AzRebit.Triggers.TimerTriggered;

internal class Setup : TriggerSetupBase
{
    public override TriggerTypes.TriggerName TriggerName => TriggerTypes.TriggerName.Timer;

    public override Type TriggerAttribute => typeof(TimerTriggerAttribute);

    public override AzFunction? TryCreateAzFunction(string functionName, TriggerBindingAttribute triggerAtribute, IServiceCollection services)
    {
        return new AzFunction(functionName, TriggerName, new Dictionary<string, string>());
    }
}
