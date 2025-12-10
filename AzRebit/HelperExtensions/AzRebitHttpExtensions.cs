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
        var container = blobServiceClient.GetBlobContainerClient(HttpMiddlewareHandler.HttpResubmitVirtualPath);
        BlobClient resubmitClient = container.GetBlobClient(resubmitFileName);
        await resubmitClient.RaiseResubmitCount();

    }
}