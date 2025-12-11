using System.Reflection;

using AzRebit.Model;
using AzRebit.Shared;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using static AzRebit.Model.TriggerTypes;

namespace AzRebit.Triggers.QueueTrigger;

internal class QueueFeatureSetup : TriggerSetupBase
{
    public override TriggerName TriggerName => TriggerName.Queue;
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
            functionMeta.Add("QueueName", queueName);

             return new AzFunction(functionName, TriggerName.Blob, functionMeta);
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected error while tyring to create az function with Queue triggert attribute "+e.Message);
            throw new AzFunctionNotCreatedException(e.Message, e);
        }
    }

}