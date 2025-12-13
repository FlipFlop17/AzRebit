using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Azure;

using NSubstitute;

namespace AzRebit.Tests;

internal static class FakesFactory
{

    internal static IAzureClientFactory<BlobServiceClient> CreateFakeAzureBlobClientFactory()
    {
        var fakeAzureClientFactory = Substitute.For<IAzureClientFactory<BlobServiceClient>>();
        BlobServiceClient serviceClient = Substitute.For<BlobServiceClient>();
        var fakeContainerClient = Substitute.For<BlobContainerClient>();
        BlobClient fakeBlobClient = Substitute.For<BlobClient>();

        fakeContainerClient.GetBlobClient(Arg.Any<string>())
        .Returns(fakeBlobClient);
        serviceClient.GetBlobContainerClient(Arg.Any<string>())
           .Returns(fakeContainerClient);

        fakeAzureClientFactory
            .CreateClient(Arg.Any<string>())
            .Returns(serviceClient);

        return fakeAzureClientFactory;
    }
    internal static BlobClient CreateFakeBlobClient()
    {
        BlobClient fakeBlobClient = Substitute.For<BlobClient>();
        var fakeBlobServiceClient = Substitute.For<BlobServiceClient>();
        var fakeContainerClient = Substitute.For<BlobContainerClient>();
        //fakeBlobClientFactory.CreateClient(Arg.Any<string>())
        //    .Returns(fakeBlobServiceClient);
        fakeBlobServiceClient.GetBlobContainerClient(Arg.Any<string>())
            .Returns(fakeContainerClient);
        fakeContainerClient.GetBlobClient(Arg.Any<string>())
            .Returns(fakeBlobClient);

        return fakeBlobClient;
    }

    internal static FunctionContext? CreateFunctionContext()
    {
        var func = Substitute.For<FunctionContext>();
        func.InvocationId.Returns(Guid.NewGuid().ToString());

        return func;
    }

    internal static HttpRequestData CreateRequestData(FunctionContext context)
    {
        var requestMock = Substitute.For<HttpRequestData>(context);
        requestMock.CreateResponse().Returns(callInfo =>
        {
            var responseMock = Substitute.For<HttpResponseData>(context);
            responseMock.Body.Returns(new MemoryStream());
            return responseMock;
        });
        return requestMock;
    }

}
