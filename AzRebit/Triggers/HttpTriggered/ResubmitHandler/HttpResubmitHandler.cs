using System.Text.Json;

using AzRebit.HelperExtensions;
using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.HttpTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Model;

using Azure.Storage.Blobs;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.HttpTriggered.Handler;

internal class HttpResubmitHandler:IResubmitHandler
{
    public const string HttpResubmitOriginalFileId = "x-resubmit-originalid";
    private readonly IHttpClientFactory _httpFact;

    public TriggerType HandlerType => TriggerType.Http;

    public HttpResubmitHandler(IHttpClientFactory httpFact)
    {
        _httpFact = httpFact;
    }
    
    public async Task<ActionResult> HandleResubmitAsync(string invocationId, object? triggerAttributeMetadata)
    {
        BlobContainerClient httpContainer = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"),HttpMiddlewareHandler.HttpResubmitContainerName);
        var blobForResubmit = await httpContainer.PickUpBlobForResubmition(invocationId);

        if (blobForResubmit is null)
        {
            return ActionResult.Failure();
        }
        var downloadResponse = await blobForResubmit.DownloadAsync();
        using var streamReader = new StreamReader(downloadResponse.Value.Content);
        var httpRequestContent =JsonSerializer.Deserialize<HttpSaveRequest>(await streamReader.ReadToEndAsync());
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

        return response.IsSuccessStatusCode ? ActionResult.Success(await response.Content.ReadAsStringAsync()) : ActionResult.Failure(await response.Content.ReadAsStringAsync());
    }
}
