using AzRebit.Triggers.BlobTriggered.Middleware;

using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AzRebit.Tests.TriggersTest.BlobTests;

[TestClass]
public class MiddlewareHandler_SaveIncoming
{
    private  ILogger<BlobMiddlewareHandler> fakeLogger;
    /// <summary>
    /// The top-level object representing the Function. Holds the runtime data
    /// Contains 'Function Definition' & 'BindingContext' of the function
    /// </summary>
    private  FunctionContext context;
    /// <summary>
    /// Inside FunctionContext - It holds the actual runtime values of the parameters used by the invoked function
    /// </summary>
    private BindingContext mockBindingContext;
    /// <summary>
    /// Inside FunctionContext - Holds the blueprint of the functions - describes what the function looks like
    /// FunctionName, MethodName, Parameters, InputBindings, OutputBindings
    /// </summary>
    private  FunctionDefinition mockFunctionDefinition;

    private BlobTriggerAttribute mockBlobTriggerAttribute;
    /// <summary>
    /// Inside the FunctionDefinition.Parameters - Its a Class representing each parameter inside the function.
    /// Holds the name, type, properties, binding atribute name
    /// </summary>
    private FunctionParameter mockFunctionParameter;
    private IAzureClientFactory<BlobServiceClient> fakeBlobClientFactory;

    public MiddlewareHandler_SaveIncoming()
    {
        fakeLogger = Substitute.For<ILogger<BlobMiddlewareHandler>>();
        context = Substitute.For<FunctionContext>();
        mockFunctionDefinition = Substitute.For<FunctionDefinition>();

        fakeBlobClientFactory = Substitute.For<IAzureClientFactory<BlobServiceClient>>();
        var fakeBlobServiceClient = Substitute.For<BlobServiceClient>();
        var fakeContainerClient = Substitute.For<BlobContainerClient>();
        fakeBlobClientFactory.CreateClient(Arg.Any<string>())
            .Returns(fakeBlobServiceClient);
        fakeBlobServiceClient.GetBlobContainerClient(Arg.Any<string>())
            .Returns(fakeContainerClient);
        mockBindingContext=Substitute.For<BindingContext>();
        mockBlobTriggerAttribute = Substitute.For<BlobTriggerAttribute>();
        mockFunctionParameter = Substitute.For<FunctionParameter>();
    }

    [TestMethod]
    [DataRow("test-invocation-id-123","TransferCats")]
    public async Task GivenAFunctionWithABlobTrigger_WhenAFunctionIsInvoked_ShouldSaveTheIncomingRequest(string invocationId,string functionName)
    {
        //arange
        mockFunctionDefinition.Name.Returns(functionName);
        context.FunctionDefinition.Returns(mockFunctionDefinition);
        context.InvocationId.Returns(invocationId);
        context.BindingContext.Returns(mockBindingContext);
        mockBlobTriggerAttribute.Connection.Returns("FakeConnectionName");
        mockBlobTriggerAttribute.BlobPath.Returns("incoming-container/blobs/test-path");

        // 2. Mock the FunctionDefinition Parameter
        
        // Setup the properties dictionary to return the mocked binding attribute
        mockFunctionParameter.Properties
            .Returns(new Dictionary<string, object>
            {
                { "bindingAttribute", mockBlobTriggerAttribute }
            });

        mockFunctionDefinition.Parameters.Returns(new List<FunctionParameter> { mockFunctionParameter });
        mockBindingContext.BindingData.Returns(new Dictionary<string, object>
        {
            // This is the key part: it holds the full path the function was triggered with
            { "BlobTrigger", "incoming-container/blobs/test-path/file-123.json" }
        });

        // 5. Mock the final FunctionContext
     
        
        var sut = new BlobMiddlewareHandler(fakeLogger, blobClientFacto);

        //act
        sut.SaveIncomingRequest();

        //assert

        //cleanup


    }


    

}
