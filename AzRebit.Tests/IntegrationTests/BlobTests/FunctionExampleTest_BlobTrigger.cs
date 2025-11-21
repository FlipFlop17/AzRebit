using System.Text;

using AwesomeAssertions;

using AzRebit.Triggers.BlobTriggered.Middleware;

using Azure.Storage.Blobs;

namespace AzRebit.Tests.IntegrationTests.BlobTests;

[TestClass]
public class FunctionExampleTest_BlobTrigger
{
    private static BlobContainerClient _blobResubmitContainerBlob;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");
        _blobResubmitContainerBlob = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, BlobMiddlewareHandler.BlobResubmitContainerName);
        await FunctionHostStarter.StartFunctionHost();
        //start the server
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        FunctionHostStarter.Dispose();
    }

    [TestMethod]
    public async Task TransferCats_WhenABlobIsAdded_ShouldSaveTheBlobReferenceForResubmition()
    {
        // Arrange
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        var inputContainer = new BlobContainerClient(connectionString, "cats-container");
        await inputContainer.CreateIfNotExistsAsync();

        var blobName = $"test-{Guid.NewGuid()}.txt";
        var originalText = "dummy-content-" + Guid.NewGuid();
        var blobClient = inputContainer.GetBlobClient(blobName);

        //create the trigger file
        using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(originalText)))
        {
            await blobClient.UploadAsync(uploadStream, overwrite: true);
        }

        await Task.Delay(10000); // wait for the function to trigger
        // Act: wait for the function to process and create the resubmitted blob
        var latestBlobFile = await _blobResubmitContainerBlob.GetBlobsAsync()
            .OrderByDescending(b => b.Properties.CreatedOn)
            .FirstAsync();
        var resubmitBlobClient = _blobResubmitContainerBlob.GetBlobClient(latestBlobFile.Name);

        var downloadResponse = await resubmitBlobClient.DownloadAsync();
        string downloadedText;
        using (var reader = new StreamReader(downloadResponse.Value.Content, Encoding.UTF8))
        {
            downloadedText = await reader.ReadToEndAsync();
        }

        downloadedText.Should().Be(originalText, because: "the content of the resubmitted blob should match the original blob content");

        // Cleanup
        await blobClient.DeleteIfExistsAsync();
        await resubmitBlobClient.DeleteIfExistsAsync();
    }
}
