using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;

namespace AzRebit.Features.ServiceBusTriggered;

internal class ServiceBusFeatureSetup : TriggerSetupBase
{
    // TEMPORARILY DISABLED FOR INITIAL LAUNCH - RE-ENABLE LATER
    private static readonly bool ServiceBusDisabled = true;

    public override TriggerType TriggerName => TriggerType.ServiceBus;
    public override Type TriggerAttribute => typeof(ServiceBusTriggerAttribute);

    public override AzFunction? TryCreateAzFunction(string functionName, TriggerBindingAttribute triggerAttribute, IServiceCollection services)
    {
        if (ServiceBusDisabled)
        {
            Console.WriteLine($"ServiceBus feature is disabled for launch. Skipping function discovery for: {functionName}");
            return null;
        }

        var functionMeta = new Dictionary<string, string>();

        try
        {
            if (triggerAttribute is not ServiceBusTriggerAttribute serviceBusAttribute)
                throw new ArgumentNullException(nameof(serviceBusAttribute), "ServiceBusTriggerAttribute is required");

            // Validate entity path (queue name or topic name)
            if (string.IsNullOrEmpty(serviceBusAttribute.QueueName) && 
                (string.IsNullOrEmpty(serviceBusAttribute.TopicName) || string.IsNullOrEmpty(serviceBusAttribute.SubscriptionName)))
            {
                throw new ArgumentException("ServiceBus queue name or topic/subscription combination must be provided");
            }

            string entityPath;
            bool isTopic = !string.IsNullOrEmpty(serviceBusAttribute.TopicName) && 
                          !string.IsNullOrEmpty(serviceBusAttribute.SubscriptionName);

            if (isTopic)
            {
                entityPath = $"{serviceBusAttribute.TopicName}/Subscriptions/{serviceBusAttribute.SubscriptionName}";
            }
            else
            {
                entityPath = serviceBusAttribute.QueueName ?? string.Empty;
            }

            var appSettingName = AssemblyDiscovery.ResolveConnectionStringAppSettingName(serviceBusAttribute.Connection);
            string connectionString = Environment.GetEnvironmentVariable(appSettingName)!;
            
            services.AddAzureClients(c =>
            {
                c.AddServiceBusClient(connectionString).WithName(functionName);
            });

            var azFunc = new AzFunction(functionName, TriggerName);
            
            if (isTopic)
            {
                if (!string.IsNullOrEmpty(serviceBusAttribute.TopicName))
                    azFunc.AddFunctionTriggerQueueName(serviceBusAttribute.TopicName);
                if (!string.IsNullOrEmpty(serviceBusAttribute.SubscriptionName))
                    azFunc.AddFunctionTriggerSubscriptionName(serviceBusAttribute.SubscriptionName);
            }
            else
            {
                if (!string.IsNullOrEmpty(serviceBusAttribute.QueueName))
                    azFunc.AddFunctionTriggerQueueName(serviceBusAttribute.QueueName);
            }

            return azFunc;
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected error while trying to create az function with ServiceBus trigger attribute: " + e.Message);
            throw new AzFunctionNotCreatedException(e.Message, e);
        }
    }
}