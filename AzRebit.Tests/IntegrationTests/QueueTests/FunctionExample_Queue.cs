using System.Text;

using AwesomeAssertions;

using AzRebit.Tests.IntegrationTests;
using AzRebit.Triggers.BlobTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Middleware;
using AzRebit.Triggers.QueueTrigger.SaveRequestMiddleware;

using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace AzRebit.Tests.IntegrationTests;

[TestClass]
public class FunctionExample_Queue
{
    public static QueueClient _inputQueueClient { get; set; }
    public static BlobContainerClient _blobContainerResubmit { get; set; }
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");
        _blobContainerResubmit = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, QueueMiddlewareHandler.ResubmitContainerNameName);
        _inputQueueClient = new QueueClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, "transform-cats-queue");
        //start the server
        await FunctionHostStarter.StartFunctionHost();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        FunctionHostStarter.Dispose();
    }

    [TestMethod]
    public async Task TransformCats_WhenTriggered_SavesBlobForResubmission()
    {

        // Arrange
        var testMessage = "test-cat-data-" + Guid.NewGuid();

        // Act
        // Send message to the queue (needs to be base64 encoded)
        await _inputQueueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(testMessage)));

        // Wait for the function to trigger and process the message
        await Task.Delay(10000); // 10 seconds delay for function processing

        // Assert
        // Get the latest blob from the resubmit container
        var latestBlob = await _blobContainerResubmit.GetBlobsAsync()
            .OrderByDescending(b => b.Properties.CreatedOn)
            .FirstAsync();

        var resubmitBlobClient = _blobContainerResubmit.GetBlobClient(latestBlob.Name);

        // Download and verify the blob content
        var downloadResponse = await resubmitBlobClient.DownloadAsync();
        string downloadedContent;
        using (var reader = new StreamReader(downloadResponse.Value.Content, Encoding.UTF8))
        {
            downloadedContent = await reader.ReadToEndAsync();
        }

        downloadedContent.Should().Be(testMessage, because: "the blob content should match the message that was sent to the queue");

        // Cleanup
        await resubmitBlobClient.DeleteIfExistsAsync();
    }
}
