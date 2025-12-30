using Azure.Storage.Blobs;
using Azure.Storage.Queues;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;

namespace AzRebitTests.IntegrationTests.QueueTriggerTest;

[Collection("FunctionApp")]
public class QueueMiddlewareHandlerTests
{
    private readonly FunctionAppFixture _host;
    private readonly ITestOutputHelper _output;
    private readonly QueueServiceClient _inputQueueClient;
    private readonly BlobContainerClient _blobResubmitContainerClient;
    private const string StringMessageQueueName = "transform-cats-string";
    private const string BinaryMessageQueueName = "transform-cats-binary";
    private const string ByteMessageQueueName = "transform-cats-byte";
    private const string QueueMessageQueueName = "transform-cats-queuemsg";

    public QueueMiddlewareHandlerTests(FunctionAppFixture host,
        ITestOutputHelper output)
    {
        _host = host;
        _output = output;
        _inputQueueClient = host.ServiceProvider
            .GetRequiredService<IAzureClientFactory<QueueServiceClient>>()
            .CreateClient("queueClient");
        _blobResubmitContainerClient = host.ServiceProvider
            .GetRequiredService<IAzureClientFactory<BlobServiceClient>>()
            .CreateClient("resubmitContainer")
            .GetBlobContainerClient("files-for-resubmit");
    }


    [Theory]
    [InlineData(QueueMessageQueueName)]
    [InlineData(StringMessageQueueName)]
    [InlineData(ByteMessageQueueName)]
    [InlineData(BinaryMessageQueueName)]
    public async Task AddQueueMessages(string queueName)
    {
        var jsonMessage = "{\"Name\": \"Whiskers\", \"Color\": \"black\"}";
        await SendMessageAsync(queueName, jsonMessage);
    }

    private async Task SendMessageAsync(string queueName, string message)
    {
        var queueClient = _inputQueueClient.GetQueueClient(queueName);
        await queueClient.SendMessageAsync(message);
        Console.WriteLine($"Sent string message to {queueName}.");
    }

    [Theory]
    [InlineData(QueueMessageQueueName, "{\"Name\": \"Whiskers\", \"Color\": \"black\"}")]
    public async Task When_Message_is_added_to_queue_Should_copy_it_in_resubmit_storage(string queueName, string queueMessage)
    {
        //arrange
        var queueClient = _inputQueueClient.GetQueueClient(queueName);
        //act
        await queueClient.SendMessageAsync(queueMessage);
        //assert

    }

}
