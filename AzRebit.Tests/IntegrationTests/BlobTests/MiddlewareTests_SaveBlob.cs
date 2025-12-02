using AzRebit.FunctionExample.Features;
using AzRebit.Middleware;
using AzRebit.Shared;
using AzRebit.Triggers.BlobTriggered.Middleware;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AzRebit.Tests;

[TestClass]
public class MiddlewareTests_SaveBlob
{

    [TestMethod]
    public async Task GivenABlobFunctionIsTriggered_WhenMiddlewareIsCalled_ShouldTriggerMiddlewareToSaveIncomingBlobFile()
    {
        //arrange
        var logger = Substitute.For<ILogger<ResubmitMiddleware>>();
        var loggerBlob = Substitute.For<ILogger<BlobMiddlewareHandler>>();

        var azureClientFactory = Substitute.For<IAzureClientFactory<BlobServiceClient>>();
        var blobResubmitContainerClient = Substitute.For<BlobContainerClient>();
        azureClientFactory
            .CreateClient(BlobMiddlewareHandler.BlobResubmitContainerName)
            .GetBlobContainerClient(BlobMiddlewareHandler.BlobResubmitContainerName)
            .Returns(blobResubmitContainerClient);
        var blobMiddlewareHandler = new BlobMiddlewareHandler(loggerBlob, azureClientFactory);
        var handlers = Enumerable.Empty<IMiddlewareHandler>(); // new List<IMiddlewareHandler>() { blobMiddlewareHandler };
        
        var sut = new ResubmitMiddleware(logger, handlers);

        //act
        await sut.Invoke(CreateDefaultFunctionContext(), CreateDefaultFunctionDelegate());
        
        //assert
    }


    private FunctionContext CreateDefaultFunctionContext()
    {
        var context = Substitute.For<FunctionContext>();
        context.InvocationId.Returns(Guid.NewGuid().ToString());
        return context;
    }

    private FunctionExecutionDelegate CreateDefaultFunctionDelegate()
    {
        var next = Substitute.For<FunctionExecutionDelegate>();
        next.Invoke(Arg.Any<FunctionContext>()).Returns(Task.CompletedTask);
        return next;
    }

}
