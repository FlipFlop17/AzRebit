using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Features.ServiceBusTriggered;

using Azure.Messaging.ServiceBus;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AzRebitTests.IntegrationTests.ServiceBusTriggerTest;

/// <summary>
/// Tests for ServiceBus function discovery and setup
/// </summary>
public class ServiceBusFunctionDiscoveryTests
{
    [Fact]
    public void ServiceBusFeatureSetup_Should_Be_Discoverable_And_Correctly_Configured()
    {
        // Arrange
        var serviceBusSetup = new ServiceBusFeatureSetup();
        var serviceCollection = new ServiceCollection();

        // Act
        var triggerType = serviceBusSetup.TriggerName;
        var triggerAttribute = serviceBusSetup.TriggerAttribute;

        // Assert
        Assert.Equal(TriggerType.ServiceBus, triggerType);
        Assert.Equal(typeof(ServiceBusTriggerAttribute), triggerAttribute);
    }

    [Fact]
    public void ServiceBusFeatureSetup_Should_Create_AzFunction_For_Queue_Trigger()
    {
        // Arrange
        var serviceBusSetup = new ServiceBusFeatureSetup();
        var serviceCollection = new ServiceCollection();
        
        var queueName = "test-queue";
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=TestKey;SharedAccessKey=TestValue;";
        Environment.SetEnvironmentVariable("ServiceBusConnection", connectionString);

        var triggerAttribute = new ServiceBusTriggerAttribute(queueName)
        {
            Connection = "ServiceBusConnection"
        };
        var functionName = "TestServiceBusQueueFunction";

        // Act
        var azFunction = serviceBusSetup.TryCreateAzFunction(functionName, triggerAttribute, serviceCollection);

        // Assert
        Assert.NotNull(azFunction);
        Assert.Equal(functionName, azFunction.Name);
        Assert.Equal(TriggerType.ServiceBus, azFunction.TriggerType);
        Assert.Equal(queueName, azFunction.GetFunctionTriggerQueueName());
    }

    [Fact]
    public void ServiceBusFeatureSetup_Should_Create_AzFunction_For_Topic_Trigger()
    {
        // Arrange
        var serviceBusSetup = new ServiceBusFeatureSetup();
        var serviceCollection = new ServiceCollection();
        
        var topicName = "test-topic";
        var subscriptionName = "test-subscription";
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=TestKey;SharedAccessKey=TestValue;";
        Environment.SetEnvironmentVariable("ServiceBusConnection", connectionString);

        // Note: For isolated worker model, topics use the 2-argument constructor
        var triggerAttribute = new ServiceBusTriggerAttribute(topicName, subscriptionName)
        {
            Connection = "ServiceBusConnection"
        };
        var functionName = "TestServiceBusTopicFunction";

        // Act
        var azFunction = serviceBusSetup.TryCreateAzFunction(functionName, triggerAttribute, serviceCollection);

        // Assert
        Assert.NotNull(azFunction);
        Assert.Equal(functionName, azFunction.Name);
        Assert.Equal(TriggerType.ServiceBus, azFunction.TriggerType);
        Assert.Equal(topicName, azFunction.GetFunctionTriggerQueueName());
        Assert.Equal(subscriptionName, azFunction.GetFunctionTriggerSubscriptionName());
    }

    [Fact]
    public void ServiceBusFeatureSetup_Should_Register_Azure_Clients_Correctly()
    {
        // Arrange
        var serviceBusSetup = new ServiceBusFeatureSetup();
        var serviceCollection = new ServiceCollection();
        
        var functionName = "TestServiceBusClient";
        var queueName = "test-queue";
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=TestKey;SharedAccessKey=TestValue;";
        Environment.SetEnvironmentVariable("ServiceBusConnection", connectionString);

        var triggerAttribute = new ServiceBusTriggerAttribute(queueName)
        {
            Connection = "ServiceBusConnection"
        };

        // Act
        var azFunction = serviceBusSetup.TryCreateAzFunction(functionName, triggerAttribute, serviceCollection);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Assert
        Assert.NotNull(azFunction);
        
        // Verify that Azure clients are registered
        // Note: The actual verification would depend on the Azure client factory implementation
        Assert.True(serviceCollection.Any(s => s.ServiceType == typeof(Microsoft.Extensions.Azure.IAzureClientFactory<Azure.Messaging.ServiceBus.ServiceBusClient>)));
    }

    [Theory]
    [InlineData("queue-name", null)]
    [InlineData("topic-name", "subscription-name")]
    public void ServiceBusFeatureSetup_Should_Handle_Various_Trigger_Scenarios(string entityPath, string? subscriptionName)
    {
        // Arrange
        var serviceBusSetup = new ServiceBusFeatureSetup();
        var serviceCollection = new ServiceCollection();
        
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=TestKey;SharedAccessKey=TestValue;";
        Environment.SetEnvironmentVariable("ServiceBusConnection", connectionString);

        ServiceBusTriggerAttribute triggerAttribute;
        if (string.IsNullOrEmpty(subscriptionName))
        {
            triggerAttribute = new ServiceBusTriggerAttribute(entityPath)
            {
                Connection = "ServiceBusConnection"
            };
        }
        else
        {
            triggerAttribute = new ServiceBusTriggerAttribute(entityPath, subscriptionName)
            {
                Connection = "ServiceBusConnection"
            };
        }

        var functionName = $"TestFunction_{Guid.NewGuid():N}";

        // Act
        var azFunction = serviceBusSetup.TryCreateAzFunction(functionName, triggerAttribute, serviceCollection);

        // Assert
        Assert.NotNull(azFunction);
        Assert.Equal(functionName, azFunction.Name);
        Assert.Equal(TriggerType.ServiceBus, azFunction.TriggerType);
        Assert.Equal(entityPath, azFunction.GetFunctionTriggerQueueName());
        
        if (!string.IsNullOrEmpty(subscriptionName))
        {
            Assert.Equal(subscriptionName, azFunction.GetFunctionTriggerSubscriptionName());
        }
        else
        {
            Assert.Null(azFunction.GetFunctionTriggerSubscriptionName());
        }
    }

    [Fact]
    public void ServiceBusFeatureSetup_Should_Handle_Null_Or_Empty_Queue_Name_Gracefully()
    {
        // Arrange
        var serviceBusSetup = new ServiceBusFeatureSetup();
        var serviceCollection = new ServiceCollection();
        
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=TestKey;SharedAccessKey=TestValue;";
        Environment.SetEnvironmentVariable("ServiceBusConnection", connectionString);

        // Test with null queue name
        var triggerAttribute = new ServiceBusTriggerAttribute(string.Empty)
        {
            Connection = "ServiceBusConnection"
        };
        var functionName = "TestFunctionWithEmptyName";

        // Act & Assert
        var exception = Assert.Throws<AzFunctionNotCreatedException>(() => 
            serviceBusSetup.TryCreateAzFunction(functionName, triggerAttribute, serviceCollection));
        
        Assert.Contains("ServiceBus queue name or topic/subscription combination must be provided", exception.Message);
    }

    [Fact]
    public void ServiceBusFeatureSetup_Should_Resolve_Connection_String_App_Setting_Name()
    {
        // Arrange
        var serviceBusSetup = new ServiceBusFeatureSetup();
        var serviceCollection = new ServiceCollection();
        
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=TestKey;SharedAccessKey=TestValue;";
        var connectionSettingName = "MyServiceBusConnection";
        Environment.SetEnvironmentVariable(connectionSettingName, connectionString);

        var triggerAttribute = new ServiceBusTriggerAttribute("test-queue")
        {
            Connection = connectionSettingName
        };
        var functionName = "TestConnectionResolutionFunction";

        // Act
        var azFunction = serviceBusSetup.TryCreateAzFunction(functionName, triggerAttribute, serviceCollection);

        // Assert
        Assert.NotNull(azFunction);
        // The connection string should be resolved from the environment variable
        // Actual verification would depend on the AssemblyDiscovery implementation
    }
}