using System.Text.Json;

using AzRebit.Infrastructure;
using AzRebit.Model;
using AzRebit.Shared;
using AzRebit.Triggers.HttpTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Model;

using Microsoft.Extensions.Logging;

using static AzRebit.Model.TriggerTypes;

namespace AzRebit.Triggers.HttpTriggered.Handler;

internal class HttpResubmitHandler:IResubmitHandler
{
    public const string HttpResubmitOriginalFileId = "x-resubmit-originalid";
    private readonly IHttpClientFactory _httpFact;
    private readonly IResubmitStorage _resubmitStorage;
    private readonly ILogger<HttpResubmitHandler> _logger;

    public TriggerName HandlerType => TriggerName.Http;

    public HttpResubmitHandler(IHttpClientFactory httpFact,IResubmitStorage resubmitStorage,ILogger<HttpResubmitHandler> logger)
    {
        _httpFact = httpFact;
        _resubmitStorage = resubmitStorage;
        _logger = logger;
    }
    
    public async Task<RebitActionResult<ResubmitHandlerResponse>> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        var blobForResubmit = await _resubmitStorage.FindAsync(invocationId);

        if (blobForResubmit is null)
        {
            return RebitActionResult<ResubmitHandlerResponse>.Failure("Cannot find the file for resubmiting");
        }
        _logger.LogInformation("Resubmiting file {FileForResubmit}",blobForResubmit.Name);
        var downloadResponse = await blobForResubmit.DownloadAsync();
        using var streamReader = new StreamReader(downloadResponse.Value.Content);
        var httpRequestContent =JsonSerializer.Deserialize<HttpRequestDto>(await streamReader.ReadToEndAsync());
        HttpClient azFuncEndpointclient = _httpFact.CreateClient();
        var httpRequestMessage = new HttpRequestMessage(new HttpMethod(httpRequestContent!.Method), httpRequestContent.Url + httpRequestContent.QueryString);
        var newInvocationId = Guid.NewGuid().ToString();
        if (!string.IsNullOrEmpty(httpRequestContent.Body))
        {
            httpRequestMessage.Content = new StringContent(httpRequestContent.Body);
        }
        if (httpRequestContent.Headers is not null)
        {
            foreach (var header in httpRequestContent.Headers)
            {
                if (header.Key.Equals(HttpMiddlewareHandler.HeaderInvocationId))
                    httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, newInvocationId);    
            }
        }
        httpRequestMessage.Headers.TryAddWithoutValidation(HttpResubmitOriginalFileId, invocationId);

        var response = await azFuncEndpointclient.SendAsync(httpRequestMessage);

        return response.IsSuccessStatusCode 
            ? RebitActionResult<ResubmitHandlerResponse>.Success(new ResubmitHandlerResponse(blobForResubmit.Name),await response.Content.ReadAsStringAsync()) 
            : RebitActionResult<ResubmitHandlerResponse>.Failure(await response.Content.ReadAsStringAsync());
    }
}
