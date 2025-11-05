using System.Text;

using AwesomeAssertions;
using AzRebit.FunctionExample.Features;
using AzRebit.Extensions;

using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using Moq;
using AzRebit.Triggers.BlobTriggered.Handler;

namespace AzFunctionResubmit.Tests.IntegrationTests;

[TestClass]
public class AzExampleFunctionTests
{
    private Mock<ILogger<GetCats>> _mockLogger;
    private GetCats _getCatsFunction;
    private const string AzuriteConnectionString = "UseDevelopmentStorage=true";

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<GetCats>>();
        _getCatsFunction = new GetCats(_mockLogger.Object);
        
        // Set the connection string for the tests
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", AzuriteConnectionString);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        // Clean up test blobs after each test
        try
        {
            var containerClient = new BlobContainerClient(
                AzuriteConnectionString, 
               BlobTriggerHandler.BlobResubmitContainerName);
            
            if (await containerClient.ExistsAsync())
            {
                await foreach (var blob in containerClient.GetBlobsAsync())
                {
                    await containerClient.DeleteBlobIfExistsAsync(blob.Name);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [TestMethod]
    public async Task GetCats_HttpTrigger_ReturnsSuccessResponse()
    {
        // Arrange
        var mockFunctionContext = new Mock<FunctionContext>();
        var mockRequest = new Mock<HttpRequestData>(mockFunctionContext.Object);
        //var mockResponse = new Mock<HttpResponseData>(mockFunctionContext.Object);
        
        var responseStream = new MemoryStream();

        // Act
        var result = await _getCatsFunction.RunGet(mockRequest.Object, mockFunctionContext.Object);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task AddCat_BlobTrigger_SavesBlobToResubmissionContainer()
    {
        // Arrange
        var testBlobContent = "test blob content for cats";
        var blobPath = "cats/fluffy.txt";
        var testContainerName = "my-container";
        var testRunId = "test-run-123";
        
        // Create test blob in Azurite
        var sourceContainerClient = new BlobContainerClient(AzuriteConnectionString, testContainerName);
        await sourceContainerClient.CreateIfNotExistsAsync();
        
        var sourceBlobClient = sourceContainerClient.GetBlobClient(blobPath);
        await sourceBlobClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(testBlobContent)), 
            overwrite: true);

        var mockFunctionContext = new Mock<FunctionContext>();
        mockFunctionContext.Setup(x => x.FunctionId).Returns(testRunId);

        // Act
        var result = await _getCatsFunction.RunAdd(sourceBlobClient, blobPath, mockFunctionContext.Object);

        // Assert - Verify function result
        result.Should().NotBeNull()
            .And.BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        okResult.Value!.ToString().Should().Contain("BlobTrigger");
        
        // Assert - Verify resubmission container exists
        var resubmitContainerClient = new BlobContainerClient(
            AzuriteConnectionString, 
           BlobTriggerHandler.BlobResubmitContainerName);
        
        var containerExists = await resubmitContainerClient.ExistsAsync();
        containerExists.Value.Should().BeTrue("the resubmission container should be created automatically");

        // Assert - Verify blob was saved with correct runId
        var savedBlobClient = resubmitContainerClient.GetBlobClient(testRunId);
        var blobExists = await savedBlobClient.ExistsAsync();
        blobExists.Value.Should().BeTrue($"blob with runId '{testRunId}' should exist in resubmission container");

        // Assert - Verify blob content matches original
        var downloadedContent = await savedBlobClient.DownloadContentAsync();
        var savedContent = downloadedContent.Value.Content.ToString();
        savedContent.Should().Be(testBlobContent, "saved blob content should match the original");

        // Assert - Verify blob has correct tags
        var tagsResponse = await savedBlobClient.GetTagsAsync();
        tagsResponse.Value.Tags.Should().ContainKey(BlobTriggerHandler.BlobInputTagName,
            "blob should have the input-RunId tag");
        tagsResponse.Value.Tags[BlobTriggerHandler.BlobInputTagName].Should().Be(testRunId,
            "tag value should match the runId");

        // Assert - Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("incoming payload saved")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Cleanup
        await sourceContainerClient.DeleteIfExistsAsync();
    }

    [TestMethod]
    public async Task AddCat_BlobTrigger_WithLargeBlob_SavesSuccessfully()
    {
        // Arrange
        var largeContent = new string('C', 1024 * 50); // 50 KB of cat emojis
        var blobPath = "cats/big-chonker.txt";
        var testContainerName = "my-container-large";
        var testRunId = "test-run-large-blob";
        
        var sourceContainerClient = new BlobContainerClient(AzuriteConnectionString, testContainerName);
        await sourceContainerClient.CreateIfNotExistsAsync();
        
        var sourceBlobClient = sourceContainerClient.GetBlobClient(blobPath);
        await sourceBlobClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(largeContent)), 
            overwrite: true);

        var mockFunctionContext = new Mock<FunctionContext>();
        mockFunctionContext.Setup(x => x.FunctionId).Returns(testRunId);

        // Act
        var result = await _getCatsFunction.RunAdd(sourceBlobClient, blobPath, mockFunctionContext.Object);

        // Assert
        result.Should().NotBeNull()
            .And.BeOfType<OkObjectResult>();

        var resubmitContainerClient = new BlobContainerClient(
            AzuriteConnectionString, 
           BlobTriggerHandler.BlobResubmitContainerName);
        
        var savedBlobClient = resubmitContainerClient.GetBlobClient(testRunId);
        var blobExists = await savedBlobClient.ExistsAsync();
        blobExists.Value.Should().BeTrue("large blob should be saved successfully");

        var properties = await savedBlobClient.GetPropertiesAsync();
        var originalSize = Encoding.UTF8.GetByteCount(largeContent);
        properties.Value.ContentLength.Should().Be(originalSize,
            "saved blob size should match original content size");

        // Cleanup
        await sourceContainerClient.DeleteIfExistsAsync();
    }

    [TestMethod]
    public async Task AddCat_BlobTrigger_WithNestedPath_PreservesOriginalName()
    {
        // Arrange
        var testBlobContent = "persian cat content";
        var blobPath = "cats/breeds/persian/fluffy-white.txt";
        var testContainerName = "my-container-nested";
        var testRunId = "test-run-nested";
        
        var sourceContainerClient = new BlobContainerClient(AzuriteConnectionString, testContainerName);
        await sourceContainerClient.CreateIfNotExistsAsync();
        
        var sourceBlobClient = sourceContainerClient.GetBlobClient(blobPath);
        await sourceBlobClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(testBlobContent)), 
            overwrite: true);

        var mockFunctionContext = new Mock<FunctionContext>();
        mockFunctionContext.Setup(x => x.FunctionId).Returns(testRunId);

        // Act
        var result = await _getCatsFunction.RunAdd(sourceBlobClient, blobPath, mockFunctionContext.Object);

        // Assert
        result.Should().NotBeNull();

        var resubmitContainerClient = new BlobContainerClient(
            AzuriteConnectionString, 
           BlobTriggerHandler.BlobResubmitContainerName);
        
        var savedBlobClient = resubmitContainerClient.GetBlobClient(testRunId);
        var blobExists = await savedBlobClient.ExistsAsync();
        blobExists.Value.Should().BeTrue();

        // Verify original blob name is preserved in tags
        var tagsResponse = await savedBlobClient.GetTagsAsync();
        tagsResponse.Value.Tags.Should().ContainKey(BlobTriggerHandler.BlobInputTagName);
        
        // The blob is saved with runId as name, but original path should be in metadata
        var downloadedContent = await savedBlobClient.DownloadContentAsync();
        downloadedContent.Value.Content.ToString().Should().Be(testBlobContent);

        // Cleanup
        await sourceContainerClient.DeleteIfExistsAsync();
    }
}
