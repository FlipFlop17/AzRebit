using AwesomeAssertions;

using AzRebit.HelperExtensions;
using AzRebit.Infrastructure;

using AzRebitTests.IntegrationTests;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;
namespace IntegrationTests.BlobTriggerTest;

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
    public async Task When_a_blob_is_created_or_updated_Should_save_it_at_resubmit_container(string functionName)
    {
        //arrange
        var blobName = $"blob-{DateTime.Now:dd_MM_yyyy_HH_mm_ss}.txt";
        var blobResubmitName = $"{functionName}/{blobName}";
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
        tags.FirstOrDefault(tag => tag.Key.Equals(IResubmitStorage.BlobTagInvocationId)).Should().NotBeNull();
    }

    [Theory]
    [InlineData("TransferCats")]
    public async Task When_a_resubmit_handler_is_invoked_Should_copy_blob_from_resubmit_container_to_functions_trigger_container(string functionName)
    {
        //arrange
        HttpClient httpClient = _httpClientFactory.CreateClient("resubmit");
        string runId = string.Empty;
        //just get any blob with invocation id
        await foreach (BlobItem blobItem in _blobResubmitContainerClient.GetBlobsAsync(BlobTraits.Tags))
        {
            blobItem.Tags.TryGetValue(IResubmitStorage.BlobTagInvocationId, out runId);
            break;
        }

        string query = $"?functionName={functionName}&invocationId={runId}";

        //act
        var resubmitResult=await httpClient.GetAsync(query);
        _testOutput.WriteLine(await resubmitResult.Content.ReadAsStringAsync());
        resubmitResult.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Given_invalid_invocationId_When_a_resubmit_handler_is_invoked_Should_return_not_found_blob()
    {
        //arrange
        HttpClient httpClient = _httpClientFactory.CreateClient("resubmit");
        string runId = "12344";
        var functionName = "TransferCats";
        string query = $"?functionName={functionName}&invocationId={runId}";

        //act
        var resubmitResult = await httpClient.GetAsync(query);
        _testOutput.WriteLine(await resubmitResult.Content.ReadAsStringAsync());
        resubmitResult.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }



}
