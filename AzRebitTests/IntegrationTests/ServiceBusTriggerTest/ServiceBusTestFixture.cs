using System.Text.Json;

using Azure.Messaging.ServiceBus;

using DotNet.Testcontainers.Containers;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.ServiceBus;

namespace AzRebitTests.IntegrationTests.ServiceBusTriggerTest;

/// <summary>
/// Test fixture for ServiceBus integration tests using Testcontainers
/// </summary>
public class ServiceBusTestFixture : IAsyncLifetime
{
    private readonly ServiceBusContainer _serviceBusContainer;
    private const int ServiceBusPort = 5671;
    private const int ServiceBusManagementPort = 5672;

    public ServiceBusTestFixture()
    {
        _serviceBusContainer = new ServiceBusBuilder()
            .WithName($"servicebus-test-{Guid.NewGuid():N}")
            .WithPortBinding(ServiceBusPort, ServiceBusPort)
            .WithPortBinding(ServiceBusManagementPort, ServiceBusManagementPort)
            .WithCleanUp(true)
            .Build();
    }

    public ServiceBusClient ServiceBusClient { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;
    public string Namespace => _serviceBusContainer.Hostname;

    public async Task InitializeAsync()
    {
        await _serviceBusContainer.StartAsync();

        // ServiceBus testcontainer typically exposes connection string via environment variable
        ConnectionString = $"Endpoint=sb://{Namespace}:{ServiceBusPort};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={GetContainerSharedAccessKey()};";
        
        ServiceBusClient = new ServiceBusClient(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        if (ServiceBusClient != null)
        {
            await ServiceBusClient.DisposeAsync();
        }

        await _serviceBusContainer.DisposeAsync();
    }

    /// <summary>
    /// Gets or creates a ServiceBus sender for a specific queue
    /// </summary>
    public ServiceBusSender CreateQueueSender(string queueName)
    {
        return ServiceBusClient.CreateSender(queueName);
    }

    /// <summary>
    /// Gets or creates a ServiceBus sender for a specific topic
    /// </summary>
    public ServiceBusSender CreateTopicSender(string topicName)
    {
        return ServiceBusClient.CreateSender(topicName);
    }

    /// <summary>
    /// Creates a queue receiver for testing message consumption
    /// </summary>
    public ServiceBusReceiver CreateQueueReceiver(string queueName, ServiceBusReceiveMode receiveMode = ServiceBusReceiveMode.PeekLock)
    {
        return ServiceBusClient.CreateReceiver(queueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = receiveMode
        });
    }

    /// <summary>
    /// Creates a subscription receiver for testing topic message consumption
    /// </summary>
    public ServiceBusReceiver CreateSubscriptionReceiver(string topicName, string subscriptionName, ServiceBusReceiveMode receiveMode = ServiceBusReceiveMode.PeekLock)
    {
        return ServiceBusClient.CreateReceiver(topicName, subscriptionName, new ServiceBusReceiverOptions
        {
            ReceiveMode = receiveMode
        });
    }

    /// <summary>
    /// Sends a test message to a queue
    /// </summary>
    public async Task SendQueueMessageAsync(string queueName, object message, string contentType = "application/json")
    {
        await using var sender = CreateQueueSender(queueName);
        var serviceBusMessage = CreateServiceBusMessage(message, contentType);
        await sender.SendMessageAsync(serviceBusMessage);
    }

    /// <summary>
    /// Sends a test message to a topic
    /// </summary>
    public async Task SendTopicMessageAsync(string topicName, object message, string contentType = "application/json")
    {
        await using var sender = CreateTopicSender(topicName);
        var serviceBusMessage = CreateServiceBusMessage(message, contentType);
        await sender.SendMessageAsync(serviceBusMessage);
    }

    /// <summary>
    /// Sends multiple messages to a queue in batch
    /// </summary>
    public async Task SendQueueMessagesAsync(string queueName, IEnumerable<object> messages, string contentType = "application/json")
    {
        await using var sender = CreateQueueSender(queueName);
        var serviceBusMessages = messages.Select(msg => CreateServiceBusMessage(msg, contentType)).ToArray();
        await sender.SendMessagesAsync(serviceBusMessages);
    }

    /// <summary>
    /// Sends multiple messages to a topic in batch
    /// </summary>
    public async Task SendTopicMessagesAsync(string topicName, IEnumerable<object> messages, string contentType = "application/json")
    {
        await using var sender = CreateTopicSender(topicName);
        var serviceBusMessages = messages.Select(msg => CreateServiceBusMessage(msg, contentType)).ToArray();
        await sender.SendMessagesAsync(serviceBusMessages);
    }

    private ServiceBusMessage CreateServiceBusMessage(object message, string contentType)
    {
        var json = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(json)
        {
            ContentType = contentType,
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString(),
            Subject = "TestMessage",
            ApplicationProperties =
            {
                ["TestProperty"] = "TestValue",
                ["TestNumber"] = 42
            }
        };

        return serviceBusMessage;
    }

    /// <summary>
    /// Helper method to get container access key (implementation depends on specific ServiceBus container)
    /// </summary>
    private string GetContainerSharedAccessKey()
    {
        // This is a placeholder implementation
        // The actual implementation would depend on the specific ServiceBus container being used
        return "FakeSharedAccessKeyForTesting";
    }
}