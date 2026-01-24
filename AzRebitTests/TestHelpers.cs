using System.Text;

using AzRebit.Features.HttpTriggered.Model;
using AzRebit.Infrastructure.FileStorage;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Castle.Core.Logging;

using Microsoft.Extensions.Azure;

using NSubstitute;

namespace AzRebitTests;

internal static class TestHelpers
{

    /// <summary>
    /// Helper method to find the latest blob without using ambiguous extension methods.
    /// This resolves the OrderByDescending/FirstAsync ambiguity error.
    /// </summary>
    public static async Task<BlobItem?> GetLatestBlobAsync(this BlobContainerClient containerClient)
    {
        BlobItem? latestBlob = null;
        DateTimeOffset latestCreationTime = DateTimeOffset.MinValue;

        // Use efficient await foreach iteration
        await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(new GetBlobsOptions { Traits = BlobTraits.Metadata }))
        {
            if (blobItem.Properties.CreatedOn.HasValue && blobItem.Properties.CreatedOn.Value > latestCreationTime)
            {
                latestCreationTime = blobItem.Properties.CreatedOn.Value;
                latestBlob = blobItem;
            }
        }
        return latestBlob;
    }

    /// <summary>
    /// creates a dummy blob for testing purposes
    /// </summary>
    /// <param name="containerClient"></param>
    /// <param name="blobName">full path of the blob. If null defaults to blobdata-{DateTime.Now:dd_MM_yyyy_HH_mm_ss}.txt</param>
    /// <returns></returns>
    public static async Task<BlobClient> CreateDummyBlob(this BlobContainerClient containerClient, 
        string? blobName = null, 
        string? blobPayload = null, 
        string? invocationIdTag = null)
    {
        var defaultBlobName = $"blobdata-{DateTime.Now:dd_MM_yyyy_HH_mm_ss}.txt";
        var blobResubmitName = blobName ?? defaultBlobName;

        var inputBlobClient = containerClient.GetBlobClient(blobResubmitName);
        byte[] data = Encoding.UTF8.GetBytes(blobPayload ?? "Dummy lorem ipsum blob");
        using var stream = new MemoryStream(data);
        //act
        var uploadResult = await inputBlobClient.UploadAsync(stream);
        if(invocationIdTag is not null)
            await inputBlobClient.SetTagsAsync(new Dictionary<string, string>() { { "InvocationId", invocationIdTag} });

        return inputBlobClient;
    }

    /// <summary>
    /// Creates a simple JSON string with dummy data
    /// </summary>
    public static string CreateDummyJson(string? customData = null)
    {
        var data = new
        {
            id = Guid.NewGuid(),
            name = "Test Item",
            value = 42,
            timestamp = DateTime.UtcNow,
            data = customData ?? "Dummy JSON data for testing"
        };

        return System.Text.Json.JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// Creates a JSON string from an object
    /// </summary>
    public static string ToJson<T>(T obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj);
    }

    /// <summary>
    /// Parses a JSON string to an object
    /// </summary>
    public static T? FromJson<T>(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// Creates a dummy HttpRequestDto for testing
    /// </summary>
    public static HttpRequestDto CreateDummyHttpRequestDto(
        string? id = null,
        string? method = null,
        string? url = null,
        string? queryString = null,
        IDictionary<string, string?>? headers = null,
        string? body = null,
        DateTime? timestampUtc = null)
    {
        return new HttpRequestDto(
            Id: id ?? Guid.NewGuid().ToString(),
            Method: method ?? "POST",
            Url: url ?? "http://host.docker.internal:7080/api/GetCats",
            QueryString: queryString,
            Headers: headers ?? new Dictionary<string, string?>
            {
                ["Content-Type"] = "application/json",
                ["Authorization"] = "Bearer test-token",
                ["User-Agent"] = "TestClient/1.0",
                ["x-azrebit-invocationid"] = Guid.NewGuid().ToString()
            },
            Body: body ?? CreateDummyJson(),
            TimestampUtc: timestampUtc ?? DateTime.UtcNow
        );
    }

    
}
