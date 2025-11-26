using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

using AzRebit.Shared;
using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Model;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker.Http;

namespace AzRebit.HelperExtensions;
public static class AzRebitHttpExtensions
{
    public static string blobConnectionString=> Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
    /// <summary>
    /// Saves the request details for future resubmission. Requests are saved as blobs in Azure Blob Storage of the function app.
    /// </summary>
    /// <param name="req">incoming request</param>
    /// <param name="contextId">the unique id of the run, usually extracted from FunctionContext</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public static async Task SaveRequestForResubmitionAsync(this HttpRequestData req, string contextId)
    {
        string resubmitFileName = $"{contextId}.json";
        using StreamReader reader = new StreamReader(req.Body);
        var requestPayload = await reader.ReadToEndAsync();
        IDictionary<string, string?>? headers = req.Headers.Any()
            ? req.Headers.ToDictionary(h => h.Key, h => h.Value != null ? string.Join(", ", h.Value) : null)
            : default;
        string path = req.Url?.AbsoluteUri ?? string.Empty;
        string queryString = req.Url?.Query ?? string.Empty;

        HttpSaveRequest requestDtoToSave = new
        (
            contextId,
            req.Method,
            path,
            queryString,
            headers,
            requestPayload,
            DateTime.UtcNow
        );
        var json = JsonSerializer.Serialize(requestDtoToSave);

        BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);
        var container = blobServiceClient.GetBlobContainerClient(HttpMiddlewareHandler.HttpResubmitContainerName);
        await container.CreateIfNotExistsAsync();
        BlobClient blobClient = container.GetBlobClient(resubmitFileName);
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await blobClient.UploadAsync(ms);
        await blobClient.SetTagsAsync(new Dictionary<string, string>
        {
            { IMiddlewareHandler.BlobTagInvocationId, contextId }
        });
        
    }

    /// <summary>
    /// When a /resubmit request is received, increments the resubmit count tag on the original saved request blob.
    /// </summary>
    /// <remarks>Does not save the request since the original request is already saved.</remarks>
    /// <param name="req"></param>
    /// <returns></returns>
    public static async Task ProcessResubmitRequest(HttpRequestData req)
    {
        req.Headers.TryGetValues(HttpResubmitHandler.HttpResubmitOriginalFileId, out var resubmitOriginalId);
        var resubmitFileName = $"{resubmitOriginalId?.First()}.json";
        BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);
        var container = blobServiceClient.GetBlobContainerClient(HttpMiddlewareHandler.HttpResubmitContainerName);
        BlobClient resubmitClient = container.GetBlobClient(resubmitFileName);
        await resubmitClient.RaiseResubmitCount();

    }
}