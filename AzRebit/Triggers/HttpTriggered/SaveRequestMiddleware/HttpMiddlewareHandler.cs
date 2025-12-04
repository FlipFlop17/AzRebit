using AzRebit.HelperExtensions;
using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.HttpTriggered.Handler;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.HttpTriggered.Middleware;

public class HttpMiddlewareHandler:IMiddlewareHandler
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
    public async Task<RebitActionResult> SaveIncomingRequest(FunctionContext context)
    {
        string invocationId = context.InvocationId;

        try
        {
            var httpRequestData = await context.GetHttpRequestDataAsync();

            if (httpRequestData is null)
            {
                return RebitActionResult.Failure("Http Request Data is null");
            }

            httpRequestData.Headers.TryGetValues(HeaderInvocationId, out var functionKeyHeader);
            if (functionKeyHeader != null)
            {
                invocationId = functionKeyHeader.First();
            }
            _logger.LogInformation(
                "Auto-saving HTTP request for resubmission with invocationId: {invocationId}",
                invocationId);

            //handle if the request is coming from the /resubmit endpoint
            if (httpRequestData.Headers.Contains(HttpResubmitHandler.HttpResubmitOriginalFileId)) 
            {
                await AzRebitHttpExtensions.ProcessResubmitRequest(httpRequestData);
            } else
            {
                await httpRequestData.SaveRequestForResubmitionAsync(invocationId);
            }

            return RebitActionResult<object>.Success(new {  InvocationId= invocationId });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error while saving incoming http request {InvocationId}",invocationId);
            return RebitActionResult.Failure(e.Message);
        }

        
    }
}
