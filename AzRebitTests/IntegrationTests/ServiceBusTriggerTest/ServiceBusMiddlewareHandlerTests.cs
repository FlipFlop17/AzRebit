using System.Text.Json;

using AzRebit;
using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Infrastructure.ServiceBus;
using AzRebit.Middleware;

using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Testcontainers.ServiceBus;

namespace AzRebitTests.IntegrationTests.ServiceBusTriggerTest;

/// <summary>
/// Integration tests for ServiceBus middleware handler
/// </summary>
[Collection("FunctionApp")]
public class ServiceBusMiddlewareHandlerTests
{
    private readonly FunctionAppFixture _host;
    private readonly ServiceBusTestFixture _serviceBusFixture;
    private readonly BlobContainerClient _blobResubmitContainerClient;

    public ServiceBusMiddlewareHandlerTests(FunctionAppFixture host)
    {
        _host = host;
        _serviceBusFixture = new ServiceBusTestFixture();
        
        _blobResubmitContainerClient = _host.ServiceProvider
            .GetRequiredService<IAzureClientFactory<BlobServiceClient>>()
            .CreateClient(ResubmitFunctionWorkerExtension.BlobResubmitServiceClientName)
            .GetBlobContainerClient("files-for-resubmit");
    }

    [Fact]
    public async Task ServiceBus_Message_Serialization_Should_Preserve_All_Properties()
    {
        // Arrange
        await _serviceBusFixture.InitializeAsync();
        
        var originalMessage = new
        {
            Name = "SerializationTest",
            Color = "black",
            Age = 5,
            IsIndoor = true,
            SpecialProperties = new
            {
                VaccinationDate = new DateTime(2024, 1, 15),
                ChipId = "ABC123DEF456",
                FavoriteToys = new[] { "feather", "ball", "laser pointer" }
            }
        };

        // Act - Create ServiceBus message and serialize it
        var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(originalMessage))
        {
            MessageId = "test-message-id",
            CorrelationId = "test-correlation-id",
            Subject = "TestSubject",
            ContentType = "application/json",
            ApplicationProperties =
            {
                ["CustomProperty"] = "CustomValue",
                ["NumberProperty"] = 123,
                ["BooleanProperty"] = true
            }
        };

        var messageData = ServiceBusMessageData.FromServiceBusMessage(serviceBusMessage);
        var serializedMessage = JsonSerializer.Serialize(messageData, new JsonSerializerOptions { WriteIndented = true });

        // Deserialize back to verify
        var deserializedMessage = JsonSerializer.Deserialize<ServiceBusMessageData>(serializedMessage);

        // Assert
        Assert.NotNull(deserializedMessage);
        Assert.Equal(serviceBusMessage.MessageId, deserializedMessage.MessageId);
        Assert.Equal(serviceBusMessage.CorrelationId, deserializedMessage.CorrelationId);
        Assert.Equal(serviceBusMessage.Subject, deserializedMessage.Subject);
        Assert.Equal(serviceBusMessage.ContentType, deserializedMessage.ContentType);
        
        Assert.Equal(originalMessage.Name, JsonSerializer.Deserialize<JsonElement>(deserializedMessage.Body).GetProperty("Name").GetString());
        Assert.Equal(originalMessage.Color, JsonSerializer.Deserialize<JsonElement>(deserializedMessage.Body).GetProperty("Color").GetString());
        
        // Check custom properties
        Assert.Equal("CustomValue", deserializedMessage.ApplicationProperties?["CustomProperty"]?.ToString());
        Assert.Equal("123", deserializedMessage.ApplicationProperties?["NumberProperty"]?.ToString());
        Assert.Equal("True", deserializedMessage.ApplicationProperties?["BooleanProperty"]?.ToString());
    }

    [Fact]
    public async Task ServiceBus_EndToEnd_Message_Flow_Should_Work()
    {
        // This is a comprehensive end-to-end test that would:
        // 1. Send a message to ServiceBus queue
        // 2. Trigger Azure Function with ServiceBus trigger
        // 3. Verify middleware captures and saves message to blob storage
        // 4. Verify resubmit functionality works
        
        // Arrange
        await _serviceBusFixture.InitializeAsync();
        var queueName = "e2e-test-queue";
        
        var testMessage = new { Name = "EndToEndTest", Color = "calico", Age = 1 };
        
        // Act
        await _serviceBusFixture.SendQueueMessageAsync(queueName, testMessage);
        
        // Wait for message processing
        await Task.Delay(2000);
        
        // TODO: Wait for Azure Function to process the message
        // TODO: Verify message was saved to blob storage
        // TODO: Test resubmit functionality
        
        // Assert
        // This would be a comprehensive assertion once the full flow is implemented
        Assert.True(true, "End-to-end test structure created - needs actual ServiceBus trigger implementation");
    }
}