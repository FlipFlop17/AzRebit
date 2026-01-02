using AwesomeAssertions;

using AzRebit;
using AzRebit.Features.BlobTriggered.SaveRequestMiddleware;
using AzRebit.Features.HttpTriggered.SaveRequestMiddleware;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Shared.Extensions;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit.Abstractions;
namespace AzRebitTests.IntegrationTests.BlobTriggerTest;

[Collection("FunctionApp")]
public class FunctionTriggeredByBlob
{
    private readonly FunctionAppFixture _functionHost;
    private readonly ITestOutputHelper _testOutput;
    BlobContainerClient _blobResubmitContainerClient;
    IHttpClientFactory _httpClientFactory;
    private BlobContainerClient _catsContainer;
    private string _blobTypePrefixCode;

    public FunctionTriggeredByBlob(FunctionAppFixture functionHost, ITestOutputHelper testOutput)
    {
        _functionHost = functionHost;
        _testOutput = testOutput;
        _blobResubmitContainerClient = _functionHost.ServiceProvider
            .GetRequiredService<IAzureClientFactory<BlobServiceClient>>()
            .CreateClient(ResubmitFunctionWorkerExtension.BlobResubmitServiceClientName)
            .GetBlobContainerClient("files-for-resubmit");
        _httpClientFactory = functionHost.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        _catsContainer = _functionHost.ServiceProvider
            .GetRequiredService<IAzureClientFactory<BlobServiceClient>>()
            .CreateClient("catsContainer")
            .GetBlobContainerClient("cats-container");
        var handler = new BlobMiddlewareHandler(Substitute.For<ILogger<BlobMiddlewareHandler>>(), Substitute.For<IResubmitStorage>());
        _blobTypePrefixCode = handler.ResubmitFilePrefix;
    }
    [Theory]
    [InlineData("TransferCats")]
    public async Task When_a_blob_is_created_or_updated_Should_save_it_at_resubmit_container(string functionName)
    {
        //arrange
        var blobName = $"transferdata-{DateTime.Now:dd_MM_yyyy_HH_mm_ss}.txt";
        var blobResubmitName = $"{functionName}/{_blobTypePrefixCode}-{blobName}";
        var inputBlobClient = _catsContainer.GetBlobClient(blobName);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("A blob has been added");
        using var stream = new MemoryStream(data);
        //act
        var uploadResult = await inputBlobClient.UploadAsync(stream);
        await Task.Delay(TimeSpan.FromSeconds(15));

        //assert
        uploadResult.Value.Should().NotBeNull();
        var blobClient = _blobResubmitContainerClient.GetBlobClient(blobResubmitName);
        var blobThere = await blobClient.ExistsAsync();
        blobThere.Value.Should().Be(true);
        var tags = await blobClient.GetClonedTagsAsync();
        tags.FirstOrDefault(tag => tag.Key.Equals(_functionHost.BlobSearchTag)).Should().NotBeNull();
        await inputBlobClient.DeleteAsync();
        await blobClient.DeleteAsync();
    }

    [Theory]
    [InlineData("TransferCats")]
    public async Task When_a_resubmit_handler_is_invoked_Should_copy_blob_from_resubmit_container_to_functions_trigger_container(string functionName)
    {
        //arrange
        HttpClient httpClient = _httpClientFactory.CreateClient("resubmit");
        string runId = string.Empty;
        //just get any blob with invocation id
        var searchPrefix = $"{functionName}/{_blobTypePrefixCode}";
        await foreach (BlobItem blobItem in _blobResubmitContainerClient.GetBlobsAsync(BlobTraits.Tags, prefix: searchPrefix))
        {
            blobItem.Tags.TryGetValue(_functionHost.BlobSearchTag, out runId);
            break;
        }

        string query = $"?functionName={functionName}&invocationId={runId}";

        //act
        var resubmitResult = await httpClient.GetAsync(query);
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
