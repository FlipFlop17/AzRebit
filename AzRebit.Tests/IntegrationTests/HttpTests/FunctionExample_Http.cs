using System.Text.Json;

using AwesomeAssertions;

using AzRebit.Triggers.BlobTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Model;

using Azure.Storage.Blobs;

using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        _blobContainerBlob = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, BlobResubmitHandler.BlobResubmitContainerName);
        _blobContainerHtttp = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, HttpResubmitHandler.HttpResubmitContainerName);
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
    public async Task Resubmit_WhenResubmitIsRequest_ShouldSuccesfullyResubmitTheRequest()
    {
        //arrange
        string invocationId = "b3cf7082-31cc-4223-8aad-fb3632ecd8f5";
        HttpClient client = FunctionHostStarter.GetHttpClient()!;

        //act
        var resubmitResponse=await client.PostAsync($"/api/resubmit?functionName=GetCats&invocationId={invocationId}", new StringContent("Requesting to resubmit the previous request"));
        resubmitResponse.EnsureSuccessStatusCode();
        //asssert
        resubmitResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        TestContext.WriteLine(await resubmitResponse.Content.ReadAsStringAsync());
    }
}
