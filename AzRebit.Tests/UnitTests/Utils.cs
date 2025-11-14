using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

using NSubstitute;

namespace AzRebit.Tests.UnitTests;

internal static class Utils
{


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
