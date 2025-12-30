using System.Text.Json;

using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Domain.Results;
using AzRebit.Features.HttpTriggered.Model;
using AzRebit.Features.HttpTriggered.SaveRequestMiddleware;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Shared.Extensions;

using Microsoft.Extensions.Logging;



namespace AzRebit.Features.HttpTriggered.ResubmitHandler;

internal class HttpResubmitHandler : IResubmitHandler
{
    public const string HttpResubmitOriginalFileId = "x-resubmit-originalid";
    private readonly IHttpClientFactory _httpFact;
    private readonly IResubmitStorage _resubmitStorage;
    private readonly ILogger<HttpResubmitHandler> _logger;

    public TriggerType HandlerType => TriggerType.Http;

    public HttpResubmitHandler(IHttpClientFactory httpFact, IResubmitStorage resubmitStorage, ILogger<HttpResubmitHandler> logger)
    {
        _httpFact = httpFact;
        _resubmitStorage = resubmitStorage;
        _logger = logger;
    }

    public async Task<RebitActionResult<ResubmitHandlerResponse>> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        try
        {
            var blobForResubmit = await _resubmitStorage.FindAsync(invocationId);

            if (blobForResubmit is null)
            {
                _logger.LogDebug("Cannot find the file for resubmiting");
                return RebitActionResult<ResubmitHandlerResponse>.Failure("Cannot find the file for resubmiting");
            }
            _logger.LogResubmitWorkData(invocationId, function.Name, blobForResubmit.Name);
            var downloadResponse = await blobForResubmit.DownloadAsync();
            using var streamReader = new StreamReader(downloadResponse.Value.Content);
            var httpRequestContent = JsonSerializer.Deserialize<HttpRequestDto>(await streamReader.ReadToEndAsync());
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
                ? RebitActionResult<ResubmitHandlerResponse>.Success(new ResubmitHandlerResponse(blobForResubmit.Name), await response.Content.ReadAsStringAsync())
                : RebitActionResult<ResubmitHandlerResponse>.Failure(await response.Content.ReadAsStringAsync());
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Unexpected error while resubmiting http request type");
            return RebitActionResult<ResubmitHandlerResponse>.Failure(e.Message);
        }

    }
}
