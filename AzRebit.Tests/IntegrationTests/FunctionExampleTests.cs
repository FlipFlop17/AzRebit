using System.Text.Json;

using AwesomeAssertions;

using AzRebit.Triggers.HttpTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Model;

using Azure.Storage.Blobs;

using Microsoft.AspNetCore.HttpsPolicy;

namespace AzRebit.Tests.IntegrationTests;

[TestClass]
public class FunctionExampleTests
{
    public static BlobContainerClient _blobContainerHtttp { get; set; }
    public static BlobContainerClient _blobContainerBlob { get; set; }
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");
        _blobContainerBlob = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, "blob-resubmits");
        _blobContainerHtttp = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, "http-resubmits");
        await FunctionHostStarter.StartFunctionHost();
        //start the server
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        FunctionHostStarter.Dispose();
    }

    [TestMethod]
    public async Task GetCats_WhenGetCatsIsCalledWithGetMethod_ShouldReturnOkWithSavedRequest()
    {
        //arrange
        HttpClient client=FunctionHostStarter.GetHttpClient()!;
        var customKey=Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add(HttpMiddlewareHandler.HeaderInvocationId, customKey);
        //act
        var response = await client.GetAsync("/api/GetCats");
        
        //assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var savedResubmisionBlob = _blobContainerHtttp.GetBlobClient(customKey+".json");
        var checkblob=await savedResubmisionBlob.ExistsAsync();
        checkblob.Value.Should().BeTrue(because:"a incoming request {0} should be saved by the middleware",customKey);
        // check body
        var blobContent = await savedResubmisionBlob.DownloadContentAsync();
        var resubmitData = JsonSerializer.Deserialize<HttpSaveRequest>(blobContent.Value.Content.ToString());
        resubmitData.Id.Should().Be(customKey);
    }
    [TestMethod]
    public async Task GetCats_WhenGetCatsIsCalledWithPostMethod_ShouldReturnOkWithSavedRequest()
    {
        //arrange
        HttpClient client = FunctionHostStarter.GetHttpClient()!;
        var customKey = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add(HttpMiddlewareHandler.HeaderInvocationId, customKey);
        var requestBody = new StringContent("Requesting to show me all available cats you have");
        //act
        var response = await client.PostAsync("/api/GetCats",requestBody);

        //assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var savedResubmisionBlob = _blobContainerHtttp.GetBlobClient(customKey + ".json");
        var checkblob = await savedResubmisionBlob.ExistsAsync();
        checkblob.Value.Should().BeTrue(because: "a incoming request {0} should be saved by the middleware", customKey);
        // check blob content
        var blobContent = await savedResubmisionBlob.DownloadContentAsync();
        var resubmitData = JsonSerializer.Deserialize<HttpSaveRequest>(blobContent.Value.Content.ToString());
        resubmitData.Body.Should().Be(await requestBody.ReadAsStringAsync());
    }
}
