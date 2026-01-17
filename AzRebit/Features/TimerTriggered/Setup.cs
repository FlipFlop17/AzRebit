using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.DependencyInjection;


namespace AzRebit.Features.TimerTriggered;

internal class Setup : TriggerSetupBase
{
    public override TriggerType TriggerName => TriggerType.Timer;

    public override Type TriggerAttribute => typeof(TimerTriggerAttribute);

    public override AzFunction TryCreateAzFunction(string functionName, TriggerBindingAttribute triggerAtribute, IServiceCollection services)
    {
        return new AzFunction(functionName, TriggerName);
    }
}
