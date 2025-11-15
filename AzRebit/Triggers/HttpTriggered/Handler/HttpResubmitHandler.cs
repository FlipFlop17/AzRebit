using System.ClientModel.Primitives;
using System.Text.Json;

using AzRebit.HelperExtensions;
using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.HttpTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Model;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.HttpTriggered.Handler;

internal class HttpResubmitHandler:ITriggerHandler
{

    public HttpResubmitHandler(IHttpClientFactory httpFact)
    {
        _httpFact = httpFact;
    }
    /// <summary>
    /// The name of the tag that is used to mark the blob file
    /// </summary>
    public const string HttpInputTagName = "input-InvocationId";
    /// <summary>
    /// Container name where the http requests are saved for resubmiting
    /// </summary>
    public const string HttpResubmitContainerName = "http-resubmits";
    private readonly IHttpClientFactory _httpFact;

    public TriggerType HandlerType => TriggerType.Http;

    public async Task<ActionResult> HandleResubmitAsync(string invocationId, object? triggerAttributeMetadata)
    {
        BlobContainerClient httpContainer = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), HttpResubmitContainerName);
        var blobForResubmit = await httpContainer.PickUpBlobForResubmition(invocationId);

        if (blobForResubmit is null)
        {
            return ActionResult.Failure();
        }
        var downloadResponse = await blobForResubmit.DownloadAsync();
        using var streamReader = new StreamReader(downloadResponse.Value.Content);
        var httpRequestContent =JsonSerializer.Deserialize<HttpSaveRequest>(await streamReader.ReadToEndAsync());
        HttpClient client = _httpFact.CreateClient();
        var httpRequestMessage = new HttpRequestMessage(new HttpMethod(httpRequestContent!.Method), httpRequestContent.Url + httpRequestContent.QueryString);
        var newInvocationId = Guid.NewGuid().ToString();
        if (httpRequestContent.Body is not null)
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
      

        var response = await client.SendAsync(httpRequestMessage);

        return response.IsSuccessStatusCode ? ActionResult.Success() : ActionResult.Failure();
    }
}
