using AzRebit.Middleware;
using AzRebit.Model;
using AzRebit.Shared;
using AzRebit.Triggers.BlobTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Middleware;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Middleware;


public static class MiddlewareHandlerFactory
{

    public static IEnumerable<object[]> GetMiddlewareHandlers
    {
        get {
            var logger = Substitute.For<ILogger<BlobMiddlewareHandler>>();
            var blobClientFacto = Substitute.For<IAzureClientFactory<BlobServiceClient>>();
            var fakeBlobServiceClient = Substitute.For<BlobServiceClient>();
            var fakeContainerClient = Substitute.For<BlobContainerClient>();
            blobClientFacto.CreateClient(Arg.Any<string>())
                .Returns(fakeBlobServiceClient);
            fakeBlobServiceClient.GetBlobContainerClient(Arg.Any<string>())
                .Returns(fakeContainerClient);

            var blobHandler=Substitute.For<IMiddlewareHandler>();
            blobHandler.BindingName.Returns("blobTrigger");
            blobHandler.SaveIncomingRequest(Arg.Any<FunctionContext>()).Returns(Task.FromResult(RebitActionResult.Success()));
            var meta = Substitute.For<BindingMetadata>();
            meta.Type.Returns("blobTrigger");
            IEnumerable<BindingMetadata> functionInputBindings = [meta];
            yield return new object[] { blobHandler, functionInputBindings };
            //
            meta = Substitute.For<BindingMetadata>();
            meta.Type.Returns("httpTrigger");
            functionInputBindings = [meta];
            var loggerHttp = Substitute.For<ILogger<HttpMiddlewareHandler>>();
            var httpHandlerFake=Substitute.For<IMiddlewareHandler>();
            httpHandlerFake.BindingName.Returns("httpTrigger");
            httpHandlerFake.SaveIncomingRequest(Arg.Any<FunctionContext>()).Returns(Task.FromResult(RebitActionResult.Success()));
            yield return new object[] { httpHandlerFake, functionInputBindings };
            //
        }
    }

}

[TestClass]
public class AzRebitMiddleware
{

    [TestMethod]
    [Description("When a middleware is actived from the Resubmit endpoint it should skip it since we are not saving request if they are from the Resubmit endpoint")]
    [DataRow("Resubmit")]
    public async Task GivenAFunctionNamedResubmitIsTriggered_WhenAMiddlewareIsActivated_ShouldReturnWithoutCallingMiddlewareHandler(string functionName)
    {
        //arrange
        var logger = Substitute.For<ILogger<ResubmitMiddleware>>();
        var handlers = Enumerable.Empty<IMiddlewareHandler>();
        var sut = new ResubmitMiddleware(logger, handlers);
        //prepare func context
        var funcContext = CreateDefaultFunctionContext();
        var funcDefinition = Substitute.For<FunctionDefinition>();
        funcDefinition.Name.Returns(functionName);
        funcContext.FunctionDefinition.Returns(funcDefinition);
        var nextDelegate = CreateDefaultFunctionDelegate();
        //act
        await sut.Invoke(funcContext, nextDelegate);
        //assert
        logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Is<EventId>(ResubmitMiddleware.SkipAutoSave),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [TestMethod]
    [Description("When a middleware is actived it should invoke the middleware handler depending on the type of trigger")]
    [DynamicData(nameof(MiddlewareHandlerFactory.GetMiddlewareHandlers),typeof(MiddlewareHandlerFactory))]
    public async Task GivenAFunctionIsTriggered_WhenAMiddlewareIsActivated_ShouldCallMiddlewareHandler(IMiddlewareHandler handlerToTest,IEnumerable<BindingMetadata> meta)
    {
        //arrange
        var logger = Substitute.For<ILogger<ResubmitMiddleware>>();
        IEnumerable<IMiddlewareHandler> handlers = [handlerToTest];
        var sut = new ResubmitMiddleware(logger, handlers);
        var funcContext = CreateDefaultFunctionContext();
        var funcDefinition = Substitute.For<FunctionDefinition>();
        funcDefinition.InputBindings.Values.Returns(meta);
        funcContext.FunctionDefinition.Returns(funcDefinition);
        var nextDelegate = CreateDefaultFunctionDelegate();
        //act
        await sut.Invoke(funcContext, nextDelegate);
        //assert check if the saveincomingpayloadmethod is calledupon
        await handlerToTest.Received(1).SaveIncomingRequest(funcContext);
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
