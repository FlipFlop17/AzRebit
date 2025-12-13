using AwesomeAssertions;

using AzRebit.HelperExtensions;
using AzRebit.IntegrationTests;
using AzRebit.Shared;
using AzRebit.Triggers.BlobTriggered.Middleware;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;

namespace AzRebit.Tests.IntegrationTests.BlobTriggerTest;

[Collection("FunctionApp")]
public class FunctionTriggeredByBlob
{
    private readonly FunctionAppFixture _functionHost;
    private readonly ITestOutputHelper _testOutput;
    BlobContainerClient _blobResubmitContainerClient;
    IHttpClientFactory _httpClientFactory;
    public FunctionTriggeredByBlob(FunctionAppFixture functionHost,ITestOutputHelper testOutput)
    {
        _functionHost = functionHost;
        _testOutput = testOutput;
        _blobResubmitContainerClient = _functionHost.ServiceProvider
            .GetRequiredService<IAzureClientFactory<BlobServiceClient>>()
            .CreateClient("resubmitContainer")
            .GetBlobContainerClient("files-for-resubmit");
        _httpClientFactory = functionHost.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    }
    [Theory]
    [InlineData("TransferCats")]
    public async Task GivenAFunctionIsTriggeredByANewBlob_WhenABlobIsCreatedOrUpdated_ShouldSaveTheBlobAtResubmitLocation(string functionName)
    {
        //arrange
        var blobName = $"blob-{Guid.NewGuid().ToString()}.txt";
        var blobResubmitName = $"{BlobMiddlewareHandler.BlobResubmitSavePath}/{functionName}/{blobName}";
        var inputBlobClient = new BlobClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"),"cats-container",blobName);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("A blob has been added");
        using var stream = new MemoryStream(data);
        //act
        var uploadResult=await inputBlobClient.UploadAsync(stream);
        await Task.Delay(TimeSpan.FromSeconds(15));
        
        //assert
        uploadResult.Value.Should().NotBeNull();
        var blobClient = _blobResubmitContainerClient.GetBlobClient(blobResubmitName);
        var blobThere = await blobClient.ExistsAsync();
        blobThere.Value.Should().Be(true);
        var tags = await blobClient.GetClonedTagsAsync();
        tags.FirstOrDefault(tag => tag.Key.Equals(ISavePayloadsHandler.BlobTagInvocationId)).Should().NotBeNull();
    }

    [Theory]
    [InlineData("TransferCats")]
    public async Task GivenAResubmitEndpointIsCalled_WhenABlobTriggeredFunctionIsSpecifiedInTheRequest_ShouldCopyTheBlobFileFromResubmitContainerToFunctionsInputContainer(string functionName)
    {
        //arrange
        HttpClient httpClient = _httpClientFactory.CreateClient("resubmit");
        string runId = string.Empty;
        await foreach (BlobItem blobItem in _blobResubmitContainerClient.GetBlobsAsync(BlobTraits.Tags))
        {
            blobItem.Tags.TryGetValue(ISavePayloadsHandler.BlobTagInvocationId, out runId);
            break;
        }

        string query = $"?functionName={functionName}&invocationId={runId}";

        //act
        var resubmitResult=await httpClient.GetAsync(query);
        _testOutput.WriteLine(await resubmitResult.Content.ReadAsStringAsync());
        resubmitResult.IsSuccessStatusCode.Should().BeTrue();
    }


}
