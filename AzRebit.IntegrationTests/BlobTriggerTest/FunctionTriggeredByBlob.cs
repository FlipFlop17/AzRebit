using AwesomeAssertions;

using AzRebit.HelperExtensions;
using AzRebit.Shared;
using AzRebit.Triggers.BlobTriggered.Middleware;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.IntegrationTests.BlobTriggerTest;

[Collection("FunctionApp")]
public class FunctionTriggeredByBlob
{
    private readonly FunctionAppFixture _functionHost;
    BlobContainerClient _blobResubmitContainerClient;
    IHttpClientFactory _httpClientFactory;
    public FunctionTriggeredByBlob(FunctionAppFixture functionHost)
    {
        _functionHost = functionHost;
        _blobResubmitContainerClient = _functionHost.ServiceProvider
            .GetRequiredService<IAzureClientFactory<BlobServiceClient>>()
            .CreateClient("resubmitContainer")
            .GetBlobContainerClient("files-for-resubmit");
        _httpClientFactory = functionHost.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    }
    [Fact]
    public async Task GivenAFunctionIsTriggeredByANewBlob_WhenABlobIsCreatedOrUpdated_ShouldSaveTheBlobAtResubmitLocation()
    {
        //arrange
        var blobName = $"blob-{Guid.NewGuid().ToString()}.txt";
        var blobResubmitName = $"{BlobMiddlewareHandler.BlobResubmitSavePath}/{blobName}";
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
    [InlineData("TransferCats", "81a23941-bf69-4493-9e3f-b1550e51bf00")]
    public async Task GivenAResubmitEndpointIsCalled_WhenABlobTriggeredFunctionIsSpecifiedInTheRequest_ShouldCopyTheBlobFileFromResubmitContainerToFunctionsInputContainer(string functionName,string runId)
    {
        //arrange
        HttpClient httpClient = _httpClientFactory.CreateClient("resubmit");
        string query = $"?functionName={functionName}&invocationId={runId}";
        var resubmitResult=await httpClient.GetAsync(query);
        Console.WriteLine(await resubmitResult.Content.ReadAsStringAsync());
        resubmitResult.IsSuccessStatusCode.Should().BeTrue();
    }

}
