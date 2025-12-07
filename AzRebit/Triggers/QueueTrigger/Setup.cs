using System.Reflection;

using AzRebit.Shared;
using AzRebit.Shared.Model;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.QueueTrigger;

internal class QueueFeatureSetup : TriggerSetupBase
{
    public override TriggerType TriggerSupport => TriggerType.Queue;

    public override AzFunction TryCreateAzFunction(string functionName, ParameterInfo[] parameters, IServiceCollection services)
    {
        QueueTriggerAttribute? queueAttribute;
        var functionMeta = new Dictionary<string, string>();
        try
        {
            var functionsTriggerParameter = parameters.FirstOrDefault(
                p => p.GetCustomAttribute<QueueTriggerAttribute>() != null
            );

            if (functionsTriggerParameter is null)
                throw new ArgumentNullException(nameof(functionsTriggerParameter));

            queueAttribute = functionsTriggerParameter.GetCustomAttribute<QueueTriggerAttribute>();

            if (queueAttribute is null)
                throw new ArgumentNullException(nameof(queueAttribute));
        }
        catch (ArgumentNullException e)
        {
            throw new AzFunctionNotCreatedException(e.Message, e);
        }

        try
        {
            var queueName = queueAttribute.QueueName;
            var connectionName = AssemblyDiscovery.ResolveConnectionString(queueAttribute.Connection);
            services.AddAzureClients(c =>
                {
                    c.AddQueueServiceClient(connectionName).WithName(functionName);
                });
            functionMeta.Add("QueueName", queueName);

             return new AzFunction(functionName, TriggerType.Blob, functionMeta);
        }
        catch (Exception)
        {
            throw;
        }
    }

}