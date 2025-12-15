using System.Net;

using AwesomeAssertions;

using AzRebit.HelperExtensions;
using AzRebit.Infrastructure;
using AzRebit.Triggers.HttpTriggered.Middleware;

using AzRebitTests.IntegrationTests;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;

namespace IntegrationTests.HttpTriggerTest;

[Collection("FunctionApp")]
public class FunctionTriggeredByHttp
{
    private FunctionAppFixture _functionHost;
    private ITestOutputHelper _testOutput;
    private BlobContainerClient _blobResubmitContainerClient;
    private IHttpClientFactory _httpClientFactory;

    public FunctionTriggeredByHttp(FunctionAppFixture functionHost, ITestOutputHelper testOutput)
    {
        _functionHost = functionHost;
        _testOutput = testOutput;
        _blobResubmitContainerClient = _functionHost.ServiceProvider
            .GetRequiredService<IAzureClientFactory<BlobServiceClient>>()
            .CreateClient("resubmitContainer")
            .GetBlobContainerClient("files-for-resubmit");
        _httpClientFactory = functionHost.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    }

    [Fact]
    public async Task When_a_http_request_is_received_Should_return_success()
    {
        //arrange
        string jsonPayload = "{ \"Id\":\"123\" }";
        HttpClient client=_httpClientFactory.CreateClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{_functionHost.BaseUrl}/api/GetCats")
        {
            Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json")
        };

        //act
        var response=await client.SendAsync(request);

        //assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
    }
    [Fact]
    public async Task Given_header_with_invocationid_When_a_http_request_is_received_Should_copy_at_resubmit_container()
    {
        //arrange
        string jsonPayload = "{ \"TestType\":\"With header id\" }";
        HttpClient client = _httpClientFactory.CreateClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{_functionHost.BaseUrl}/api/GetCats")
        {
            Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json")
        };
        var customInvocationId=Guid.NewGuid().ToString();
        request.Headers.Add(HttpMiddlewareHandler.HeaderInvocationId, customInvocationId);
        //act
        var response = await client.SendAsync(request);
        response.IsSuccessStatusCode.Should().BeTrue();
        //assert
        _testOutput.WriteLine($"{customInvocationId}.http.json");
        var blob=_blobResubmitContainerClient.GetBlobClient($"GetCats/{customInvocationId}.http.json");
        (await blob.ExistsAsync()).Value.Should().Be(true);
        //check for tags as well
        (await blob.GetClonedTagsAsync())
        .Should()
        .Contain(IResubmitStorage.BlobTagInvocationId, customInvocationId);

    }
}
