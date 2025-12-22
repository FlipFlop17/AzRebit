using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;



namespace AzRebit.Features.QueueTriggered;

internal class QueueFeatureSetup : TriggerSetupBase
{
    public override TriggerType TriggerName => TriggerType.Queue;
    public override Type TriggerAttribute => typeof(QueueTriggerAttribute);
    public override AzFunction TryCreateAzFunction(string functionName, TriggerBindingAttribute triggerAttribute, IServiceCollection services)
    {
        var functionMeta = new Dictionary<string, string>();

        try
        {
            if (triggerAttribute is not QueueTriggerAttribute queueAttribute) throw new ArgumentNullException();

            var queueName = queueAttribute.QueueName;
            var appSettingName = AssemblyDiscovery.ResolveConnectionStringAppSettingName(queueAttribute.Connection);
            string connectionString = Environment.GetEnvironmentVariable(appSettingName)!;
            services.AddAzureClients(c =>
            {
                c.AddQueueServiceClient(connectionString).WithName(functionName);
            });
            var azFunc = new AzFunction(functionName, TriggerName);
            azFunc.AddFunctionTriggerQueueName(queueName);
            return azFunc;
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected error while tyring to create az function with Queue triggert attribute "+e.Message);
            throw new AzFunctionNotCreatedException(e.Message, e);
        }
    }

}