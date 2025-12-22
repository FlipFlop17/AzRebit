using AzRebit.Infrastructure.FileStorage;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace AzRebit.Shared.Extensions;
public static class AzRebitBlobExtensions
{    
    /// <summary>
    /// Deletes a saved blob from the resubmission container
    /// </summary>
    /// <param name="invocationId">The uniqueue id of the execution. Usually get from <c>FunctionContext.InvocationId</c></param>
    /// <returns></returns>
    public static async Task<bool> DeleteSavedResubmitionBlobAsync(string invocationId)
    {
        var containerClient = new BlobContainerClient(
            Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
            BlobResubmitStorage.ResubmitContainerName);

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
       this BlobBaseClient blobClient,
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
