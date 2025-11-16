using AzRebit.HelperExtensions;
using AzRebit.Shared;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.HttpTriggered.Middleware;

internal class HttpMiddlewareHandler:IMiddlewareHandler
{
    private readonly ILogger<HttpMiddlewareHandler> _logger;

    /// <summary>
    /// Container name where the http requests are saved for resubmiting
    /// </summary>
    public const string HttpResubmitContainerName = "http-resubmits";
    public string BindingName => "httpTrigger";
    public const string HeaderInvocationId = "x-azrebit-invocationid"; //optional header to specify custom invocation id
    //public const string HeaderResubmitFlag = "x-azrebit-resubmit"; //optional header to specify this is a resubmission request
    public HttpMiddlewareHandler(ILogger<HttpMiddlewareHandler> logger)
    {
        _logger = logger;
    }

   
    /// <summary>
    /// Saves the incoming HTTP request before the users endpoint starts processing for potential resubmission later.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
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
