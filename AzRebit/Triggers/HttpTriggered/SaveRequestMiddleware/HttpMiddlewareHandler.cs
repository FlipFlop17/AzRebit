using System.Text.Json;

using AzRebit.HelperExtensions;
using AzRebit.Infrastructure;
using AzRebit.Model;
using AzRebit.Shared;
using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Model;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.HttpTriggered.Middleware;

public class HttpMiddlewareHandler:ISavePayloadsHandler
{
    private readonly ILogger<HttpMiddlewareHandler> _logger;
    private readonly IResubmitStorage _resubmitStorage;

    /// <summary>
    /// Container name where the http requests are saved for resubmiting
    /// </summary>
    public const string HttpResubmitVirtualPath = "http-resubmits";
    public string BindingName => "httpTrigger";
    public const string HeaderInvocationId = "x-azrebit-invocationid"; //optional header to specify custom invocation id
    //public const string HeaderResubmitFlag = "x-azrebit-resubmit"; //optional header to specify this is a resubmission request
    public HttpMiddlewareHandler(ILogger<HttpMiddlewareHandler> logger, IResubmitStorage resubmitStorage)
    {
        _logger = logger;
        _resubmitStorage = resubmitStorage;
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

                var payloadToSave= await PrepareHttpRequestForSaveAsync(httpRequestData,invocationId);
                var destinationPath = $"{HttpResubmitVirtualPath}/{context.FunctionDefinition.Name}/{invocationId}.http.json";
                
                await _resubmitStorage.SaveFileAtResubmitLocation(payloadToSave, destinationPath);
            }

            return RebitActionResult<object>.Success(new {  InvocationId= invocationId });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error while saving incoming http request {InvocationId}",invocationId);
            return RebitActionResult.Failure(e.Message);
        }

        
    }
    private async Task<string> PrepareHttpRequestForSaveAsync(HttpRequestData req,string invocationId)
    {
        using StreamReader reader = new StreamReader(req.Body);
        var requestPayload = await reader.ReadToEndAsync();
        IDictionary<string, string?>? headers = req.Headers.Any()
            ? req.Headers.ToDictionary(h => h.Key, h => h.Value != null ? string.Join(", ", h.Value) : null)
            : default;
        string path = req.Url?.AbsoluteUri ?? string.Empty;
        string queryString = req.Url?.Query ?? string.Empty;

        var requestDtoToSave = new HttpRequestDto
        (
            invocationId,
            req.Method,
            path,
            queryString,
            headers,
            requestPayload,
            DateTime.UtcNow
        );
        var json = JsonSerializer.Serialize(requestDtoToSave);

        return json;
    }
}
