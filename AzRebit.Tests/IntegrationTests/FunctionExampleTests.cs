using Azure.Storage.Blobs;

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
    public async Task GetCats_WhenGetCatsIsCalled_ShouldReturnHttpResponseDataWithAllAvailableCats()
    {
        //arrange
        HttpClient client=FunctionHostStarter.GetHttpClient()!;

        var response = await client.GetAsync("/api/GetCats");
        //act
        TestContext.WriteLine(await response.Content.ReadAsStringAsync());
        //assert
        //check response data
        //assert --check saved request

    }
}
