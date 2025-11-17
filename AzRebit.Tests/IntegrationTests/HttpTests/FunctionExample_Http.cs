using System.Text.Json;

using AwesomeAssertions;

using AzRebit.HelperExtensions;
using AzRebit.Shared;
using AzRebit.Triggers.BlobTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Model;

using Azure.Storage.Blobs;

namespace AzRebit.Tests.IntegrationTests.HttpTests;

[TestClass]
public class FunctionExample_Http
{
    public static BlobContainerClient _blobContainerHtttp { get; set; }
    public static BlobContainerClient _blobContainerBlob { get; set; }
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");
        _blobContainerBlob = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, BlobMiddlewareHandler.BlobResubmitContainerName);
        _blobContainerHtttp = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, HttpMiddlewareHandler.HttpResubmitContainerName);
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
        resubmitData?.Id.Should().Be(customKey);
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
        resubmitData?.Body.Should().Be(await requestBody.ReadAsStringAsync());
    }

    [TestMethod]
    [DataRow("GetCats", "2c7bae55-564c-4d6e-b66a-5cef2c20e732")]
    public async Task Resubmit_WhenResubmitIsRequest_ShouldSuccesfullyResubmitTheRequest(string functionName,string invocationId)
    {
        //arrange
        HttpClient client = FunctionHostStarter.GetHttpClient()!;
        //check current resubmit count from blob
        var blobFile = _blobContainerHtttp.GetBlobClient(invocationId+".json");
        var currentResubmitCount=await blobFile.GetCurrentResubmitCount();
        //act
        var resubmitResponse=await client.PostAsync($"/api/resubmit?functionName={functionName}&invocationId={invocationId}", new StringContent("Requesting to resubmit the previous request"));
        resubmitResponse.EnsureSuccessStatusCode();
        //asssert
        resubmitResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var newResubmitCount = await blobFile.GetCurrentResubmitCount();
        newResubmitCount.Should().NotBeNull();
        newResubmitCount.Should().Be(currentResubmitCount + 1,because:"we resubmited the file and resubmit counter should be +1");
        TestContext.WriteLine(await resubmitResponse.Content.ReadAsStringAsync());
    }
}
