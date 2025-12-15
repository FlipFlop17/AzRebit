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

public class HttpMiddlewareHandler:ISavePayloadHandler
{
    private readonly ILogger<HttpMiddlewareHandler> _logger;
    private readonly IResubmitStorage _resubmitStorage;
    public string BindingName => "httpTrigger";
    /// <summary>
    /// Headers that mark that the invocation id should be taken from the header and not of the FunctionContext
    /// </summary>
    public const string HeaderInvocationId = "x-azrebit-invocationid"; 
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
    public async Task<RebitActionResult> SaveIncomingRequest(ISavePayloadCommand command)
    {
        string invocationId = command.Context.InvocationId;

        try
        {
            var httpRequestData = await command.Context.GetHttpRequestDataAsync();

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
                var destinationPath = $"{command.Context.FunctionDefinition.Name}/{invocationId}.http.json";
                
                await _resubmitStorage.SaveFileAtResubmitLocation(payloadToSave, 
                    destinationPath, 
                    new Dictionary<string, string>() { { IResubmitStorage.BlobTagInvocationId, invocationId } });
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
