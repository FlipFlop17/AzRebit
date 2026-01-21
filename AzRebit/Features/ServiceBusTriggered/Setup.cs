using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Shared;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Features.ServiceBusTriggered;

/// <summary>
/// Setup is needed for the assembly discovery process to find and register this feature
/// </summary>
internal class Setup : TriggerSetupBase
{
    public override TriggerType TriggerName => TriggerType.ServiceBus;
    public override Type TriggerAttribute => typeof(ServiceBusTriggerAttribute);
    public override AzFunction TryCreateAzFunction(string functionName, TriggerBindingAttribute triggerAttribute, IServiceCollection services)
    {
        try
        {
            if (triggerAttribute is not ServiceBusTriggerAttribute serviceBusAttr) 
                throw new Exception("Trigger binding attribute is null");

            var functionMeta = new Dictionary<string, string>();
            var connectionName = AssemblyDiscovery.ResolveConnectionStringAppSettingName(serviceBusAttr.Connection);
            string connectionString = Environment.GetEnvironmentVariable(connectionName)!;
            
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddServiceBusClientWithNamespace(connectionString)
                    .WithName(functionName);
            });
            
            var azFunc = new AzFunction(functionName, TriggerType.ServiceBus);
            
            // Store queue or topic information
            if (!string.IsNullOrEmpty(serviceBusAttr.QueueName))
            {
                azFunc.AddFunctionTriggerContainerName(serviceBusAttr.QueueName);
            }
            else if (!string.IsNullOrEmpty(serviceBusAttr.TopicName) && !string.IsNullOrEmpty(serviceBusAttr.SubscriptionName))
            {
                azFunc.AddFunctionTriggerContainerName($"{serviceBusAttr.TopicName}/{serviceBusAttr.SubscriptionName}");
            }
            
            return azFunc;
        }
        catch (Exception e)
        {
            throw new AzFunctionNotCreatedException(e.Message, e);
        }
    }
}