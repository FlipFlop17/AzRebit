using System.Text.Json;

using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Results;
using AzRebit.Features.HttpTriggered.Model;
using AzRebit.Infrastructure.FileStorage;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.HttpTriggered.SaveRequestMiddleware;

internal class HttpMiddlewareHandler : ISavePayloadHandler
{
    private readonly ILogger<HttpMiddlewareHandler> _logger;
    private readonly IResubmitStorage _resubmitStorage;

    internal HttpMiddlewareHandler(ILogger<HttpMiddlewareHandler> logger, IResubmitStorage resubmitStorage)
    {
        _logger = logger;
        _resubmitStorage = resubmitStorage;
    }

    /// <summary>
    /// Prefix added when saving file to resubmit storage
    /// </summary>
    public string ResubmitFilePrefix => "t_http";

    /// <summary>
    /// Headers that marks that the invocation id should be taken from the header and not of the FunctionContext
    /// </summary>
    public const string HeaderInvocationId = "x-azrebit-invocationid";
    public string BindingName => "httpTrigger";

    /// <summary>
    /// Saves the incoming HTTP request before the users endpoint starts processing for potential resubmission later.
    /// </summary>
    /// <param name="command"></param>
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

            var payloadToSave = await PrepareHttpRequestForSaveAsync(httpRequestData, invocationId);
            var destinationPath = $"{command.Context.FunctionDefinition.Name}/{ResubmitFilePrefix}-{invocationId}.json";

            await _resubmitStorage.SaveFileAtResubmitLocation(payloadToSave,
                destinationPath,
                invocationId);


            return RebitActionResult<object>.Success(new { InvocationId = invocationId });
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Unexpected error while saving incoming http request {InvocationId}", invocationId);
            return RebitActionResult.Failure(e.Message);
        }

    }
    private async Task<string> PrepareHttpRequestForSaveAsync(HttpRequestData req, string invocationId)
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
