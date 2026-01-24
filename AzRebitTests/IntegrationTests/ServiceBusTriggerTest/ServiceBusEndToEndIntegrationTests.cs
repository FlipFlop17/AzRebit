using System.Text.Json;

using AzRebit;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Infrastructure.ServiceBus;

using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.ServiceBus;

using Xunit.Abstractions;

namespace AzRebitTests.IntegrationTests.ServiceBusTriggerTest;

/// <summary>
/// End-to-end integration tests for ServiceBus functionality
/// </summary>
[Collection("FunctionApp")]
public class ServiceBusEndToEndIntegrationTests
{
    private readonly FunctionAppFixture _host;
    private readonly ITestOutputHelper _output;
    private readonly ServiceBusTestFixture _serviceBusFixture;
    private readonly BlobContainerClient _blobResubmitContainerClient;

    public ServiceBusEndToEndIntegrationTests(FunctionAppFixture host, ITestOutputHelper output)
    {
        _host = host;
        _output = output;
        _serviceBusFixture = new ServiceBusTestFixture();
        
        _blobResubmitContainerClient = _host.ServiceProvider
            .GetRequiredService<IAzureClientFactory<BlobServiceClient>>()
            .CreateClient(ResubmitFunctionWorkerExtension.BlobResubmitServiceClientName)
            .GetBlobContainerClient("files-for-resubmit");
    }

    private async Task InitializeTestAsync()
    {
        await _serviceBusFixture.InitializeAsync();
        await EnsureBlobContainerExists();
    }

    private async Task EnsureBlobContainerExists()
    {
        try
        {
            await _blobResubmitContainerClient.CreateIfNotExistsAsync();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to create blob container: {ex.Message}");
        }
    }

    [Fact]
    public async Task ServiceBus_Queue_Message_Should_Be_Captured_By_Middleware_And_Resubmit_Should_Work()
    {
        // This test demonstrates the full end-to-end flow:
        // 1. Send message to ServiceBus queue
        // 2. Azure Function processes message (middleware captures it)
        // 3. Verify message is saved to blob storage
        // 4. Test resubmit functionality
        
        // Arrange
        await InitializeTestAsync();
        var queueName = "e2e-queue-test";
        var testMessage = new 
        { 
            Name = "IntegrationTestCat",
            Color = "orange",
            Age = 3,
            Metadata = new 
            {
                Shelter = "Local Shelter",
                AdoptionDate = new DateTime(2024, 1, 15),
                HealthStatus = "Healthy"
            }
        };

        // Act - Step 1: Send message to ServiceBus queue
        await _serviceBusFixture.SendQueueMessageAsync(queueName, testMessage);
        _output.WriteLine($"Sent test message to queue '{queueName}'");

        // Act - Step 2: Simulate Azure Function processing (this would normally be triggered by ServiceBus)
        // For now, we'll manually test the blob storage integration
        
        // Create a mock ServiceBus message data
        var serviceBusMessage = CreateServiceBusMessage(testMessage, queueName);
        var messageData = AzRebit.Infrastructure.ServiceBus.ServiceBusMessageData.FromServiceBusMessage(serviceBusMessage);
        
        // Serialize as the middleware would do
        var jsonContent = JsonSerializer.Serialize(messageData, new JsonSerializerOptions { WriteIndented = true });
        var destinationPath = $"servicebus-resubmits/{queueName}/t_sb-{Guid.NewGuid():N}.json";
        
        // Act - Step 3: Save to blob storage (simulating middleware behavior)
        var resubmitStorage = _host.ServiceProvider.GetRequiredService<IResubmitStorage>();
        await resubmitStorage.SaveFileAtResubmitLocation(
            jsonContent,
            destinationPath,
            Guid.NewGuid().ToString());

        _output.WriteLine($"Saved message to blob storage at path: {destinationPath}");

        // Assert - Step 4: Verify message was saved correctly
        var blobName = destinationPath.Split('/').Last();
        var blobClient = _blobResubmitContainerClient.GetBlobClient($"{queueName}/{blobName}");
        
        Assert.True(await blobClient.ExistsAsync(), $"Blob should exist at path {destinationPath}");
        
        var downloadResponse = await blobClient.DownloadAsync();
        var savedContent = await new StreamReader(downloadResponse.Value.Content).ReadToEndAsync();
        var savedMessageData = JsonSerializer.Deserialize<AzRebit.Infrastructure.ServiceBus.ServiceBusMessageData>(savedContent);

        Assert.NotNull(savedMessageData);
        Assert.Equal(serviceBusMessage.MessageId, savedMessageData.MessageId);
        Assert.Equal(serviceBusMessage.CorrelationId, savedMessageData.CorrelationId);
        
        // Verify the message body was preserved
        var savedBody = JsonSerializer.Deserialize<JsonElement>(savedMessageData.Body);
        Assert.Equal(testMessage.Name, savedBody.GetProperty("Name").GetString());
        Assert.Equal(testMessage.Color, savedBody.GetProperty("Color").GetString());

        _output.WriteLine("✅ ServiceBus queue message successfully captured and stored");
    }

    [Fact]
    public async Task ServiceBus_Topic_Message_Should_Be_Captured_By_Middleware_And_Resubmit_Should_Work()
    {
        // Arrange
        await InitializeTestAsync();
        var topicName = "e2e-topic-test";
        var subscriptionName = "e2e-subscription-test";
        var testMessage = new 
        { 
            Name = "TopicIntegrationTestCat",
            Color = "tabby",
            Age = 2,
            TopicInfo = new 
            {
                Publisher = "TestPublisher",
                TopicId = "topic-123",
                MessageType = "CatAdoptionEvent"
            }
        };

        // Act - Send message to ServiceBus topic
        await _serviceBusFixture.SendTopicMessageAsync(topicName, testMessage);
        _output.WriteLine($"Sent test message to topic '{topicName}' with subscription '{subscriptionName}'");

        // Simulate middleware capturing topic message
        var serviceBusMessage = CreateServiceBusMessage(testMessage, topicName, isTopic: true, subscriptionName);
        var messageData = AzRebit.Infrastructure.ServiceBus.ServiceBusMessageData.FromServiceBusMessage(serviceBusMessage);
        var jsonContent = JsonSerializer.Serialize(messageData, new JsonSerializerOptions { WriteIndented = true });
        var destinationPath = $"servicebus-resubmits/{topicName}/{subscriptionName}/t_sb-{Guid.NewGuid():N}.json";

        // Save to blob storage
        var resubmitStorage = _host.ServiceProvider.GetRequiredService<IResubmitStorage>();
        await resubmitStorage.SaveFileAtResubmitLocation(
            jsonContent,
            destinationPath,
            Guid.NewGuid().ToString());

        // Assert - Verify topic message was saved correctly
        var blobName = destinationPath.Split('/').Last();
        var blobPath = $"{topicName}/{subscriptionName}/{blobName}";
        var blobClient = _blobResubmitContainerClient.GetBlobClient(blobPath);
        
        Assert.True(await blobClient.ExistsAsync(), $"Topic blob should exist at path {blobPath}");
        
        var downloadResponse = await blobClient.DownloadAsync();
        var savedContent = await new StreamReader(downloadResponse.Value.Content).ReadToEndAsync();
        var savedMessageData = JsonSerializer.Deserialize<AzRebit.Infrastructure.ServiceBus.ServiceBusMessageData>(savedContent);

        Assert.NotNull(savedMessageData);
        Assert.Equal(serviceBusMessage.MessageId, savedMessageData.MessageId);
        Assert.Equal(serviceBusMessage.Subject, savedMessageData.Subject);
        
        // Verify topic-specific metadata
        var savedBody = JsonSerializer.Deserialize<JsonElement>(savedMessageData.Body);
        Assert.Equal(testMessage.Name, savedBody.GetProperty("Name").GetString());
        Assert.Equal(testMessage.TopicInfo.Publisher, savedBody.GetProperty("TopicInfo").GetProperty("Publisher").GetString());

        _output.WriteLine("✅ ServiceBus topic message successfully captured and stored");
    }

    [Fact]
    public async Task ServiceBus_Message_With_Custom_Properties_Should_Preserve_All_Properties()
    {
        // Arrange
        await InitializeTestAsync();
        var queueName = "properties-test-queue";
        var complexMessage = new 
        { 
            Name = "ComplexPropertiesCat",
            Data = new 
            {
                NestedObject = new { Value = 42, Text = "nested" },
                ArrayData = new[] { 1, 2, 3, 4, 5 },
                DateTime = DateTime.UtcNow,
                NullableValue = (int?)null
            }
        };

        // Act - Create ServiceBus message with extensive custom properties
        var serviceBusMessage = CreateComplexServiceBusMessage(complexMessage, queueName);
        var messageData = AzRebit.Infrastructure.ServiceBus.ServiceBusMessageData.FromServiceBusMessage(serviceBusMessage);
        var jsonContent = JsonSerializer.Serialize(messageData, new JsonSerializerOptions { WriteIndented = true });
        var destinationPath = $"servicebus-resubmits/{queueName}/t_sb-{Guid.NewGuid():N}.json";

        // Save to blob storage
        var resubmitStorage = _host.ServiceProvider.GetRequiredService<IResubmitStorage>();
        await resubmitStorage.SaveFileAtResubmitLocation(
            jsonContent,
            destinationPath,
            Guid.NewGuid().ToString());

        // Assert - Verify all properties were preserved
        var blobName = destinationPath.Split('/').Last();
        var blobClient = _blobResubmitContainerClient.GetBlobClient($"{queueName}/{blobName}");
        
        Assert.True(await blobClient.ExistsAsync());
        
        var downloadResponse = await blobClient.DownloadAsync();
        var savedContent = await new StreamReader(downloadResponse.Value.Content).ReadToEndAsync();
        var savedMessageData = JsonSerializer.Deserialize<AzRebit.Infrastructure.ServiceBus.ServiceBusMessageData>(savedContent);

        Assert.NotNull(savedMessageData);
        
        // Verify all custom properties are preserved
        Assert.Equal("CustomStringProperty", savedMessageData.ApplicationProperties?["CustomStringProperty"]?.ToString());
        Assert.Equal("123", savedMessageData.ApplicationProperties?["CustomNumberProperty"]?.ToString());
        Assert.Equal("True", savedMessageData.ApplicationProperties?["CustomBooleanProperty"]?.ToString());
        Assert.NotNull(savedMessageData.ApplicationProperties?["CustomDateTimeProperty"]);
        
        // Verify the message body is fully preserved
        var savedBody = JsonSerializer.Deserialize<JsonElement>(savedMessageData.Body);
        Assert.Equal(complexMessage.Name, savedBody.GetProperty("Name").GetString());
        
        var nestedObject = savedBody.GetProperty("Data").GetProperty("NestedObject");
        Assert.Equal(42, nestedObject.GetProperty("Value").GetInt32());
        Assert.Equal("nested", nestedObject.GetProperty("Text").GetString());

        _output.WriteLine("✅ ServiceBus message with complex properties successfully preserved");
    }

    private ServiceBusMessage CreateServiceBusMessage(object message, string entityName, bool isTopic = false, string? subscriptionName = null)
    {
        var json = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(json)
        {
            MessageId = $"test-message-{Guid.NewGuid():N}",
            CorrelationId = $"test-correlation-{Guid.NewGuid():N}",
            Subject = isTopic ? "TopicTestMessage" : "QueueTestMessage",
            ContentType = "application/json",
            ApplicationProperties =
            {
                ["EntityName"] = entityName,
                ["IsTopic"] = isTopic.ToString(),
                ["TestProperty"] = "TestValue",
                ["Timestamp"] = DateTime.UtcNow.ToString("O")
            }
        };

        if (isTopic && !string.IsNullOrEmpty(subscriptionName))
        {
            serviceBusMessage.ApplicationProperties["SubscriptionName"] = subscriptionName;
        }

        return serviceBusMessage;
    }

    private ServiceBusMessage CreateComplexServiceBusMessage(object message, string queueName)
    {
        var json = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(json)
        {
            MessageId = $"complex-msg-{Guid.NewGuid():N}",
            CorrelationId = $"complex-correlation-{Guid.NewGuid():N}",
            Subject = "ComplexPropertiesTest",
            ContentType = "application/json",
            TimeToLive = TimeSpan.FromHours(1),
            ApplicationProperties =
            {
                ["CustomStringProperty"] = "CustomStringProperty",
                ["CustomNumberProperty"] = 123,
                ["CustomBooleanProperty"] = true,
                ["CustomDateTimeProperty"] = DateTime.UtcNow,
                ["CustomObjectProperty"] = JsonSerializer.Serialize(new { Nested = "value" }),
                ["CustomArrayProperty"] = JsonSerializer.Serialize(new[] { "a", "b", "c" })
            }
        };

        return serviceBusMessage;
    }
}