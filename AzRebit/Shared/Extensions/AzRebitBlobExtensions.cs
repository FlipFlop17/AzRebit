using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace AzRebit.Shared.Extensions;

/// <summary>
/// Provides extension methods for working with Azure Blob storage clients, including utilities for extracting blob
/// names and directory paths, combining blob path segments, and retrieving copies of blob tags and metadata.
/// </summary>
/// <remarks>These extension methods are designed to simplify common operations when interacting with Azure Blob
/// storage. All methods are static and can be called directly on instances of BlobClient or BlobBaseClient. Returned
/// dictionaries from metadata and tag retrieval methods are copies and can be safely modified without affecting the
/// underlying blob data.</remarks>
public static class AzRebitBlobExtensions
{


    /// <summary>
    /// Extracts the blob name (last segment after last forward slash)
    /// </summary>
    /// <param name="blobClient"></param>
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
    /// <param name="blobClient"></param>
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

    /// <summary>
    /// Asynchronously retrieves a copy of the metadata associated with the specified blob.
    /// </summary>
    /// <remarks>The returned dictionary is a copy of the blob's metadata. Modifying the returned dictionary
    /// does not affect the metadata stored in the blob.</remarks>
    /// <param name="blobClient">The <see cref="BlobClient"/> instance representing the blob from which to retrieve metadata.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary with the blob's
    /// metadata key-value pairs. Returns an empty dictionary if the blob has no metadata or if metadata is not
    /// supported.</returns>
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
