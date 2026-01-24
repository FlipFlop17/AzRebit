using System.Text.Json;

using AwesomeAssertions;

using AzRebit;
using AzRebit.Features.HttpTriggered.SaveRequestMiddleware;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Shared.Extensions;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;


using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit.Abstractions;

namespace AzRebitTests.IntegrationTests.HttpTriggerTest;

[Collection("FunctionApp")]
public class FunctionTriggeredByHttp
{
    private FunctionAppFixture _functionHost;
    private ITestOutputHelper _testOutput;
    private BlobContainerClient _blobResubmitContainerClient;
    private IHttpClientFactory _httpClientFactory;
    private string _httpPrefixCode { get; }

    public FunctionTriggeredByHttp(FunctionAppFixture functionHost, ITestOutputHelper testOutput)
    {
        _functionHost = functionHost;
        _testOutput = testOutput;
        _blobResubmitContainerClient = _functionHost.ServiceProvider
            .GetRequiredService<IAzureClientFactory<BlobServiceClient>>()
            .CreateClient(ResubmitFunctionWorkerExtension.BlobResubmitServiceClientName)
            .GetBlobContainerClient("files-for-resubmit");
        _httpClientFactory = functionHost.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var handler = new HttpMiddlewareHandler(Substitute.For<ILogger<HttpMiddlewareHandler>>(), Substitute.For<IResubmitStorage>());
        _httpPrefixCode = handler.ResubmitFilePrefix;
    }

    [Fact]
    public async Task When_a_http_request_is_received_Should_return_success()
    {
        //arrange
        string jsonPayload = "{ \"Id\":\"123\" }";
        HttpClient client = _httpClientFactory.CreateClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{_functionHost.BaseUrl}/api/GetCats")
        {
            Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json")
        };

        //act
        var response = await client.SendAsync(request);
        //assert
        _testOutput.WriteLine(await response.Content.ReadAsStringAsync());
        response.IsSuccessStatusCode.Should().BeTrue();

    }
    [Fact]
    public async Task Given_header_with_invocationid_When_a_http_request_is_received_Should_copy_at_resubmit_container()
    {
        //arrange
        string functionName = "GetCats";
        string jsonPayload = "{ \"TestType\":\"With header id\" }";
        HttpClient client = _httpClientFactory.CreateClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{_functionHost.BaseUrl}/api/{functionName}")
        {
            Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json")
        };
        var customInvocationId = Guid.NewGuid().ToString();
        request.Headers.Add(HttpMiddlewareHandler.HeaderInvocationId, customInvocationId);
        //act
        var response = await client.SendAsync(request);
        response.IsSuccessStatusCode.Should().BeTrue();
        
        //assert
        _testOutput.WriteLine($"{functionName}/{_httpPrefixCode}-{customInvocationId}.json");
        var blob = _blobResubmitContainerClient.GetBlobClient($"{functionName}/{_httpPrefixCode}-{customInvocationId}.json");
        (await blob.ExistsAsync()).Value.Should().Be(true);
        //check for tags as well
        (await blob.GetClonedTagsAsync())
        .Should()
        .Contain(_functionHost.BlobSearchTag, customInvocationId);

    }
    [Fact]
    public async Task When_resubmit_handler_is_invoked_should_create_new_http_request_to_azure_function()
    {
        string functionName = "GetCats";
        //arrange
        HttpClient httpResubmitClient = _httpClientFactory.CreateClient("resubmit");
        string blobName = $"{functionName}/{_httpPrefixCode}-tc-transfercats{DateTime.Now:dd_MM_yyyy_HH_mm_ss}.json";
        string runId = Guid.NewGuid().ToString();
        string payload = JsonSerializer.Serialize(TestHelpers.CreateDummyHttpRequestDto());
        var blobClient = await _blobResubmitContainerClient.CreateDummyBlob(blobName,
            blobPayload: payload, 
            invocationIdTag: runId);

        string query = $"?functionName={functionName}&invocationId={runId}";
        _testOutput.WriteLine(query);
        //act
        var resubmitResult = await httpResubmitClient.GetAsync(query);
        _testOutput.WriteLine(await resubmitResult.Content.ReadAsStringAsync());
        //assert
        resubmitResult.IsSuccessStatusCode.Should().BeTrue();

        //teardown
        await blobClient.DeleteIfExistsAsync();
    }
}
