using System.Text;
using System.Text.Json;

using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Model;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker.Http;

namespace AzRebit.Extensions;
public static class AzRebitHttpExtensions
{

    /// <summary>
    /// Saves the request details for future resubmission. Requests are saved as blobs in Azure Blob Storage of the function app.
    /// </summary>
    /// <param name="req">incoming request</param>
    /// <param name="id">the unique id of the run, usually extracted from FunctionContext</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public static async Task SaveRequestForResubmitionAsync(this HttpRequestData req,string id)
    {
        var blobConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrWhiteSpace(blobConnectionString))
            throw new ArgumentException("AzureWebJobsStorage environment variable is not defined");
        
        using StreamReader reader = new StreamReader(req.Body);
        var requestPayload = await reader.ReadToEndAsync();
        var headers =req.Headers.Any() ?  req.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()):default;
        string path = req.Url?.AbsolutePath ?? string.Empty;
        string queryString = req.Url?.Query ?? string.Empty;
        HttpSaveRequest requestDtoToSave = new
        (
            id,
            req.Method,
            path,
            queryString,
            headers,
            requestPayload,
            DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(requestDtoToSave);

        BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);
        var container=blobServiceClient.GetBlobContainerClient(HttpTriggerHandler.HttpResubmitContainerName);
        await container.CreateIfNotExistsAsync();
        BlobClient blobClient = container.GetBlobClient(id+".json");
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await blobClient.UploadAsync(ms);
        await blobClient.SetTagsAsync(new Dictionary<string, string>
        {
            { HttpTriggerHandler.HttpInputTagName, id }
        });
    }

}