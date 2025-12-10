using System.Text.Json;

using AzRebit.Infrastructure;
using AzRebit.Model;
using AzRebit.Shared;
using AzRebit.Triggers.HttpTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Model;

using static AzRebit.Model.TriggerTypes;

namespace AzRebit.Triggers.HttpTriggered.Handler;

internal class HttpResubmitHandler:IResubmitHandler
{
    public const string HttpResubmitOriginalFileId = "x-resubmit-originalid";
    private readonly IHttpClientFactory _httpFact;
    private readonly IResubmitStorage _resubmitStorage;

    public TriggerName HandlerType => TriggerName.Http;

    public HttpResubmitHandler(IHttpClientFactory httpFact,IResubmitStorage resubmitStorage)
    {
        _httpFact = httpFact;
        _resubmitStorage = resubmitStorage;
    }
    
    public async Task<RebitActionResult> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        var blobForResubmit = await _resubmitStorage.FindAsync(invocationId);

        if (blobForResubmit is null)
        {
            return RebitActionResult.Failure("Cannot find the file for resubmiting");
        }
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
            ? RebitActionResult.Success(await response.Content.ReadAsStringAsync()) 
            : RebitActionResult.Failure(await response.Content.ReadAsStringAsync());
    }
}
