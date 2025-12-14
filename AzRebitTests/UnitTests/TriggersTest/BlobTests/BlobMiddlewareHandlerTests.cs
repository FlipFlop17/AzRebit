using System.ComponentModel;

using AwesomeAssertions;

using AzRebit.Infrastructure;
using AzRebit.Middleware;
using AzRebit.Triggers.BlobTriggered.Middleware;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit.Abstractions;

namespace UnitTests.Triggers.BlobTest;

public class BlobMiddlewareHandlerTests
{
    public ITestOutputHelper TestOutput { get; set; }
    private  ILogger<BlobMiddlewareHandler> fakeLogger;
    /// <summary>
    /// The top-level object representing the Function. Holds the runtime data
    /// Contains 'Function Definition' & 'BindingContext' of the function
    /// </summary>
    private  FunctionContext context;
    /// <summary>
    /// Inside FunctionContext - It holds the actual runtime values of the parameters used by the invoked function
    /// </summary>
    private BindingContext? mockBindingContext;
    /// <summary>
    /// Inside FunctionContext - Holds the blueprint of the functions - describes what the function looks like
    /// FunctionName, MethodName, Parameters, InputBindings, OutputBindings
    /// </summary>
    private  FunctionDefinition mockFunctionDefinition;
    private IResubmitStorage resubmitStorage;

    /// <summary>
    /// Inside the FunctionDefinition.Parameters - Its a Class representing each parameter inside the function.
    /// Holds the name, type, properties, binding atribute name
    /// </summary>
    private FunctionParameter? mockFunctionParameter;
    private IAzureClientFactory<BlobServiceClient> fakeBlobClientFactory;
    private BlobClient fakeBlobClient;

    public BlobMiddlewareHandlerTests()
    {
        fakeLogger = Substitute.For<ILogger<BlobMiddlewareHandler>>();
        context = Substitute.For<FunctionContext>();
        mockFunctionDefinition=Substitute.For<FunctionDefinition>();
        fakeBlobClient = Substitute.For<BlobClient>();

        resubmitStorage = Substitute.For<IResubmitStorage>();
        resubmitStorage
            .SaveFileAtResubmitLocation(fakeBlobClient,"fakePath")
            .Returns(Task.FromResult(true));

        fakeBlobClientFactory = Substitute.For<IAzureClientFactory<BlobServiceClient>>();
        var fakeBlobServiceClient = Substitute.For<BlobServiceClient>();
        var fakeContainerClient = Substitute.For<BlobContainerClient>();
        fakeBlobClientFactory.CreateClient(Arg.Any<string>())
            .Returns(fakeBlobServiceClient);
        fakeBlobServiceClient.GetBlobContainerClient(Arg.Any<string>())
            .Returns(fakeContainerClient);
        fakeContainerClient.GetBlobClient(Arg.Any<string>())
            .Returns(fakeBlobClient);

        var contextFeatures = Substitute.For<IInvocationFeatures>();
        // Mock the IFunctionInputBindingFeature
        var inputBindingFeature = Substitute.For<IFunctionInputBindingFeature>();

        var functionInputBindingResult = new FunctionInputBindingResult([fakeBlobClient]);

        // Setup the binding feature to return the correct type
        inputBindingFeature.BindFunctionInputAsync(Arg.Any<FunctionContext>())
            .Returns(ValueTask.FromResult(functionInputBindingResult));

        // Wire up the features to the context
        contextFeatures.Get<IFunctionInputBindingFeature>()
            .Returns(inputBindingFeature);
        context.Features.Returns(contextFeatures);
    }

    [Theory]
    [InlineData("test-invocation-id-123","TransferCats")]
    public async Task GivenAFunctionWithABlobTrigger_WhenAFunctionIsInvoked_ShouldSaveTheIncomingRequest(string invocationId,string functionName)
    {
        //arange
        mockFunctionDefinition.Name.Returns(functionName);
        context.FunctionDefinition.Returns(mockFunctionDefinition);
        context.InvocationId.Returns(invocationId);
       
        var sut = new BlobMiddlewareHandler(fakeLogger, resubmitStorage);

        // //act
        var blobSaveResult=await sut.SaveIncomingRequest(new SavePayloadCommand(context));

        //assert
        blobSaveResult.IsSuccess.Should().BeTrue();
    }

}
