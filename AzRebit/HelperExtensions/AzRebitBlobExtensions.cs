using AzRebit.Shared;
using AzRebit.Triggers.BlobTriggered.Middleware;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzRebit.HelperExtensions;
public static class AzRebitBlobExtensions
{    

    /// <summary>
    /// Tries to get the current resubmit count from blob tag named ResubmitCount
    /// </summary>
    /// <param name="client"></param>
    /// <returns>null or int</returns>
    internal static async Task<int?> GetCurrentResubmitCount(this BlobClient client)
    {
        string tagKey = IMiddlewareHandler.BlobTagResubmitCount;
        var existingTags = await client.GetTagsAsync();
        if (existingTags.Value is null || existingTags.Value.Tags is null)
            return null;

        if (!existingTags.Value.Tags.TryGetValue(tagKey, out var resubmitCountValue))
        {
            return null;
        }
        return int.TryParse(resubmitCountValue, out var resubmitCount) ? resubmitCount : null;
    }

    /// <summary>
    /// if <c>ResubmitCount</c> tag is available it will raise the count and update the tag
    /// </summary>
    /// <param name="client"></param>
    /// <param name="createTag">defaults to <c>true</c>to create the tag if there is none. If false then method will do nothing</param>
    /// <param name="setTo">optionalyy pass in a fixed value</param>
    /// <returns>final count</returns>
    internal static async Task<IDictionary<string,string>> RaiseResubmitCount(this BlobClient client,bool createTag=true, int? setTo=null)
    {
        string tagKey = IMiddlewareHandler.BlobTagResubmitCount;
        var existingTags = await client.GetTagsAsync();
        if (existingTags.Value is null || existingTags.Value.Tags is null)
            return new Dictionary<string,string>();
        
        if (!existingTags.Value.Tags.TryGetValue(tagKey, out var resubmitCountValue))
        {
            if (!createTag)
                return existingTags.Value.Tags;
        }

        var allTags = existingTags.Value.Tags;
        if (setTo is not null)
        {
            allTags[tagKey] = setTo.ToString();
        }else
        {
            bool isParsedOk=int.TryParse(resubmitCountValue,out int currentCount);
            if (!isParsedOk) 
                currentCount = 0;

            allTags[tagKey] = (currentCount + 1).ToString();
        }
        
        await client.SetTagsAsync(allTags);
        return allTags;
    }

    /// <summary>
    /// Deletes a saved blob from the resubmission container
    /// </summary>
    /// <param name="invocationId">The uniqueue id of the execution. Usually get from <c>FunctionContext.InvocationId</c></param>
    /// <returns></returns>
    public static async Task<bool> DeleteSavedResubmitionBlobAsync(string invocationId)
    {
        var containerClient = new BlobContainerClient(
            Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
            BlobMiddlewareHandler.BlobResubmitSavePath);

        if (!await containerClient.ExistsAsync())
        {
            return false;
        }

        var blobClient = containerClient.GetBlobClient(invocationId);
        return await blobClient.DeleteIfExistsAsync();
    }


    /// <summary>
    /// Extracts the blob name (last segment after last forward slash)
    /// </summary>
    /// <param name="blobPath">Full blob path including virtual folders</param>
    /// <returns>Blob name with extension</returns>
    public static string GetBlobName(this BlobClient blobClient)
    {
        if (string.IsNullOrEmpty(blobClient.Name))
            return blobClient.Name;

        int lastSlashIndex = blobClient.Name.LastIndexOf('/');
        return lastSlashIndex >= 0 ? blobClient.Name.Substring(lastSlashIndex + 1) : blobClient.Name;
    }

    /// <summary>
    /// Extracts the directory path (everything before the last forward slash)
    /// </summary>
    /// <param name="blobPath">Full blob path including virtual folders</param>
    /// <returns>Directory path without blob name, empty string if no path</returns>
    public static string GetBlobDirectoryPath(this BlobClient blobClient)
    {
        if (string.IsNullOrEmpty(blobClient.Name))
            return string.Empty;

        int lastSlashIndex = blobClient.Name.LastIndexOf('/');
        return lastSlashIndex >= 0 ? blobClient.Name.Substring(0, lastSlashIndex) : string.Empty;
    }

    /// <summary>
    /// Combines path segments into a blob path using forward slashes
    /// </summary>
    public static string CombineBlobPath(params string[] segments)
    {
        if (segments == null || segments.Length == 0)
            return string.Empty;

        return string.Join("/", segments.Where(s => !string.IsNullOrEmpty(s)));
    }

    /// <summary>
    /// Gets tags
    /// </summary>
    /// <param name="blobClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<IDictionary<string, string>> GetClonedTagsAsync(
       this BlobClient blobClient,
       CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await blobClient.GetTagsAsync(cancellationToken: cancellationToken);

            return response.Value?.Tags != null
                ? new Dictionary<string, string>(response.Value.Tags)
                : new Dictionary<string, string>();
        }
        catch (RequestFailedException)
        {
            // If tags are not supported or missing, return empty
            return new Dictionary<string, string>();
        }
    }

    public static async Task<IDictionary<string, string>> GetClonedMetadataAsync(
      this BlobClient blobClient,
      CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            return response.Value?.Metadata != null
                ? new Dictionary<string, string>(response.Value.Metadata)
                : new Dictionary<string, string>();
        }
        catch (RequestFailedException)
        {
            // If meta are not supported or missing, return empty
            return new Dictionary<string, string>();
        }
    }


}
