using System.Text.Json;

using AzRebit.FunctionExample.Features;
using AzRebit.Tests.UnitTests;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AzRebit.Tests;

[TestClass]
public class FunctionExampleTests
{
    public static BlobContainerClient _blobContainerHtttp { get; set; }
    public static BlobContainerClient _blobContainerBlob { get; set; }
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        //Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");
        _blobContainerBlob = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, "blob-resubmits");
        _blobContainerHtttp = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, "http-resubmits");
        //start the server
    }

    [TestMethod]
    public async Task GetCats_GetAllAvailableCats_ShouldReturnHttpResponseDataWithAllAvailableCats()
    {
        //arrange

        //act

        //assert
        //check response data
        //assert --check saved request

    }
}
