using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using AwesomeAssertions;

using AzRebit.FunctionExample.Features;
using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Middleware;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AzRebit.Tests.UnitTests;

[TestClass]
public sealed class FunctionExampleTests
{
    public TestContext? TestContext { get; set; }
    public static BlobContainerClient _blobContainerHtttp { get; set; }
    public static BlobContainerClient _blobContainerBlob { get; set; }


    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");
        _blobContainerBlob = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, "blob-resubmits");
        _blobContainerHtttp = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, "http-resubmits");
    }
    [TestMethod]
    public async Task GetCats_GetAllAvailableCats_ShouldSaveIncomingHttpRequest()
    {
        //arrange
        var functionContextMock = Utils.CreateFunctionContext();
        var getCatsRequestMock = Utils.CreateRequestData(functionContextMock);
        var loggerMock = Substitute.For<ILogger<Cats>>();
        var catsClass = new Cats(loggerMock);
        var blobContainer = HttpMiddlewareHandler.HttpResubmitContainerName;
        var blobStorage=new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!, blobContainer);
        //act
        var getCatsResult = await catsClass.RunGet(getCatsRequestMock, functionContextMock);

        //assert --check saved request
        getCatsResult.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        //test ok if this request is saved on the local blob storage
        var savedBlobFile=_blobContainerHtttp.GetBlobClient(functionContextMock.InvocationId);
        savedBlobFile.Should().NotBeNull();



    }
}
