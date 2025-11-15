using AzRebit.HelperExtensions;
using AzRebit.Shared;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.HttpTriggered.Middleware;

internal class HttpMiddlewareHandler:IMiddlewareHandler
{
    private readonly ILogger<HttpMiddlewareHandler> _logger;

    public HttpMiddlewareHandler(ILogger<HttpMiddlewareHandler> logger)
    {
        _logger = logger;
    }

    public string BindingName => "httpTrigger";
    public const string HeaderInvocationId = "x-azrebit-invocationid";
    public async Task SaveIncomingRequest(FunctionContext context)
    {
        var httpRequestData = await context.GetHttpRequestDataAsync();

        if (httpRequestData != null)
        {
            string invocationId = context.InvocationId;
            httpRequestData.Headers.TryGetValues(HeaderInvocationId, out var functionKeyHeader);
            if (functionKeyHeader != null)
            {
                invocationId = functionKeyHeader.First();
            }
            _logger.LogInformation(
                "Auto-saving HTTP request for resubmission with invocationId: {invocationId}",
                invocationId);

            // Automatically save the request for resubmission
            await httpRequestData.SaveRequestForResubmitionAsync(invocationId);
        }
        
    }
}
