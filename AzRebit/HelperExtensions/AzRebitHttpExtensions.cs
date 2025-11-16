using System.Text;
using System.Text.Json;

using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Model;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker.Http;

namespace AzRebit.HelperExtensions;
public static class AzRebitHttpExtensions
{

    /// <summary>
    /// Saves the request details for future resubmission. Requests are saved as blobs in Azure Blob Storage of the function app.
    /// </summary>
    /// <param name="req">incoming request</param>
    /// <param name="contextId">the unique id of the run, usually extracted from FunctionContext</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public static async Task SaveRequestForResubmitionAsync(this HttpRequestData req, string contextId)
    {
        var blobConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrWhiteSpace(blobConnectionString))
            throw new ArgumentException("AzureWebJobsStorage environment variable is not defined");
        string resubmitFileName = $"{contextId}.json";
        using StreamReader reader = new StreamReader(req.Body);
        var requestPayload = await reader.ReadToEndAsync();
        var headers = req.Headers.Any() ? req.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)) : default;
        string path = req.Url?.AbsolutePath ?? string.Empty;
        string queryString = req.Url?.Query ?? string.Empty;
        //check if we are saving a request that is a resubmit itself
        req.Headers.TryGetValues(HttpResubmitHandler.HttpResubmitCountTag, out var resubmitFlag);
        if (resubmitFlag is not null)
        {
            //received a resubmit request - we dont need to save it just iterate the retry count
            int resubmitTryCount = int.Parse(resubmitFlag.First());
            req.Headers.TryGetValues(HttpResubmitHandler.HttpResubmitOriginalFileId, out var resubmitOriginalId);
            resubmitFileName = $"{resubmitOriginalId?.First()}.json";
            BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);
            var container = blobServiceClient.GetBlobContainerClient(HttpMiddlewareHandler.HttpResubmitContainerName);
            BlobClient resubmitClient = container.GetBlobClient(resubmitFileName);
            //update blob count tag
            await resubmitClient.UpdateBlobTag(HttpResubmitHandler.HttpResubmitCountTag, (resubmitTryCount + 1).ToString());

        } else { 

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
            var container=blobServiceClient.GetBlobContainerClient(HttpMiddlewareHandler.HttpResubmitContainerName);
            await container.CreateIfNotExistsAsync();
            BlobClient blobClient = container.GetBlobClient(resubmitFileName);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(ms);
            await blobClient.SetTagsAsync(new Dictionary<string, string>
            {
                { HttpMiddlewareHandler.HttpInputTagName, contextId }
            });
        }
    }

}