using System;
using System.Collections.Generic;
using System.Text;

using AzRebitTests.IntegrationTests;

using Azure.Storage.Queues;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;

namespace IntegrationTests.QueueTriggerTest;

[Collection("FunctionApp")]
public class QueueMiddlewareHandlerTests
{
    private readonly FunctionAppFixture _host;
    private readonly ITestOutputHelper _output;
    private readonly QueueServiceClient _inputQueueClient;
    private const string StringMessageQueueName = "transform-cats-string";
    private const string BinaryMessageQueueName = "transform-cats-binary";
    private const string ByteMessageQueueName = "transform-cats-byte";
    private const string QueueMessageQueueName = "transform-cats-queuemsg";

    public QueueMiddlewareHandlerTests(FunctionAppFixture host,ITestOutputHelper output)
    {
        _host = host;
        _output = output;
        _inputQueueClient = host.ServiceProvider
            .GetRequiredService<IAzureClientFactory<QueueServiceClient>>()
            .CreateClient("queueClient");
    }


    [Fact]
    public async Task AddQueueMessages()
    {
        var stringMessage = "This is a simple text message.";
        var jsonMessage = "{\"name\": \"Whiskers\", \"type\": \"cat\"}";
        var binaryPayload = Encoding.UTF8.GetBytes("This is raw binary content.");
        // --- 1. STRING QUEUE: Send a plain text message ---
        await SendMessageAsync(StringMessageQueueName, stringMessage);
        // --- 2. QUEUE MESSAGE QUEUE: Send a JSON string (will be read via QueueMessage) ---
        await SendMessageAsync(QueueMessageQueueName, jsonMessage);
    }

    private async Task SendMessageAsync(string queueName, string message)
    {
        var queueClient =_inputQueueClient.GetQueueClient(queueName);

        // Send the message. The SDK handles Base64 encoding.
        await queueClient.SendMessageAsync(message);
        Console.WriteLine($"Sent string message to {queueName}.");
    }
}
