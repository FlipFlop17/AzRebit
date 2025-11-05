using System.Text;

using AzRebit.Triggers.BlobTriggered.Handler;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace AzRebit.Extensions;
public static class AzRebitBlobExtensions
{

    /// <summary>
    /// Saves the blob in a local container for resubmition
    /// </summary>
    /// <param name="blobClient"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static async Task SaveBlobForResubmitionAsync(this BlobBaseClient blobClient,string id)
    {
        var existingTagsResponse = await blobClient.GetTagsAsync();
        IDictionary<string, string> tags=new Dictionary<string,string>();
        
        if (existingTagsResponse.Value is not null)
        {
            tags = existingTagsResponse.Value.Tags;
        }
        BlobContainerClient localContainer = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), BlobTriggerHandler.BlobResubmitContainerName);
        await localContainer.CreateIfNotExistsAsync();
        BlobClient destinationClient= localContainer.GetBlobClient(id);
        var operation=await destinationClient.StartCopyFromUriAsync(blobClient.Uri);
        await operation.WaitForCompletionAsync();
        //set tags
        tags[BlobTriggerHandler.BlobInputTagName] = id;
        await destinationClient.SetTagsAsync(tags);
    }

    /// <summary>
    /// Saves blob content directly to the resubmission container
    /// Used for primitive type bindings (string, byte[], Stream, POCO)
    /// </summary>
    /// <param name="content">The blob content as a stream</param>
    /// <param name="blobName">The original blob name (used for metadata)</param>
    /// <param name="id">The run ID for tracking</param>
    /// <param name="contentType">Optional content type of the blob</param>
    /// <returns></returns>
    public static async Task SaveBlobContentForResubmissionAsync(
        Stream content,
        string blobName,
        string id,
        string? contentType = null)
    {
        BlobContainerClient localContainer = new BlobContainerClient(
            Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
            BlobTriggerHandler.BlobResubmitContainerName);

        await localContainer.CreateIfNotExistsAsync();

        BlobClient destinationClient = localContainer.GetBlobClient(id);

        // Upload the content
        var uploadOptions = new BlobUploadOptions();
        if (!string.IsNullOrEmpty(contentType))
        {
            uploadOptions.HttpHeaders = new BlobHttpHeaders { ContentType = contentType };
        }

        // Reset stream position if possible
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await destinationClient.UploadAsync(content, uploadOptions);

        // Set tags with original blob name and run ID
        var tags = new Dictionary<string, string>
        {
            [BlobTriggerHandler.BlobInputTagName] = id,
            ["OriginalBlobName"] = blobName
        };

        await destinationClient.SetTagsAsync(tags);
    }

    /// <summary>
    /// Saves blob content directly to the resubmission container (byte[] overload)
    /// </summary>
    public static async Task SaveBlobContentForResubmissionAsync(
        byte[] content,
        string blobName,
        string id,
        string? contentType = null)
    {
        using var memoryStream = new MemoryStream(content);
        await SaveBlobContentForResubmissionAsync(memoryStream, blobName, id, contentType);
    }

    /// <summary>
    /// Saves blob content directly to the resubmission container (string overload)
    /// </summary>
    public static async Task SaveBlobContentForResubmissionAsync(
        string content,
        string blobName,
        string id,
        string? contentType = "text/plain")
    {
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await SaveBlobContentForResubmissionAsync(memoryStream, blobName, id, contentType);
    }
    /// <summary>
    /// Searches for blob inside the given container that has the tag "input-invocationId" with the specified id value.
    /// </summary>
    /// <param name="id">Unique id usually extracted from FunctionContext.FunctionId</param>
    /// <returns>null if not blob is found</returns>
    internal static async Task<BlobClient?> PickUpBlobForResubmition(this BlobContainerClient container,string id)
    {
        // Use tag filtering for more efficient search
        string tagFilter = $"\"{BlobTriggerHandler.BlobInputTagName}\" = '{id}'";


        await foreach (TaggedBlobItem taggedBlob in container.FindBlobsByTagsAsync(tagFilter))
        {
            // Return the first matching blob (should be only one)
            return container.GetBlobClient(taggedBlob.BlobName);
        }

        return null;
    }

    /// <summary>
    /// Deletes a saved blob from the resubmission container
    /// </summary>
    public static async Task DeleteSavedBlobAsync(string invocationId)
    {
        var containerClient = new BlobContainerClient(
            Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
            BlobTriggerHandler.BlobResubmitContainerName);

        if (!await containerClient.ExistsAsync())
        {
            return; // Container doesn't exist, nothing to delete
        }

        var blobClient = containerClient.GetBlobClient(invocationId);
        await blobClient.DeleteIfExistsAsync();
    }

    /// <summary>
    /// Moves the blob to a new location inside the existing storage account using a server-side copy (copy by URI).
    /// If a new path is provided, the blob is moved to that path within the same container.
    /// If the path contains a new name, the blob is renamed accordingly otherwise it retains its original name.
    /// Tags are preserved during the move.
    /// </summary>
    /// <param name="blobFileClient"></param>
    /// <param name="path">Destination blob path (blob name within the same container)</param>
    /// <param name="delete">Whether to delete the source blob after copying. Default is true.</param>
    /// <returns>The BlobClient for the newly copied blob</returns>
    public static async Task<BlobClient> MoveBlobAsync(BlobClient blobSource, string path,bool delete=true)
    {
        //TODO: Adjust the method so tags are uploaded with blob options becase this way we have two operations

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("path cannot be empoty");
        }
        // Fetch the container client from the source blob client
        var containerClient = blobSource.GetParentBlobContainerClient();

        // Normalize path
        var normalizedPath = path.Replace('\\', '/').TrimStart('/');

        string destinationPath;
        // Get last segment and check for extension
        var lastSegment = normalizedPath.Contains('/') ? normalizedPath[(normalizedPath.LastIndexOf('/') + 1)..] : normalizedPath;
        var hasExtension = !string.IsNullOrEmpty(Path.GetExtension(lastSegment));

        if (hasExtension)
        {
            // Path already contains a filename with extension -> use as-is
            destinationPath = normalizedPath;
        }
        else
        {
            // Treat normalizedPath as a directory -> append source filename
            // Ensure single '/' between directory and filename
            destinationPath = normalizedPath.EndsWith('/') ? $"{normalizedPath}{blobSource.Name}" : $"{normalizedPath}/{blobSource.Name}";
        }
        

        // Create destination blob client for the computed path inside the same container
        var destBlobClient = containerClient.GetBlobClient(destinationPath);

        // If source and destination are the same, nothing to do
        if (string.Equals(blobSource.Name, destBlobClient.Name, StringComparison.Ordinal))
        {
            return destBlobClient;
        }

        var copyOperation = await destBlobClient.StartCopyFromUriAsync(blobSource.Uri);

        await copyOperation.WaitForCompletionAsync();

        var tagsResponse = await blobSource.GetTagsAsync();
        var tags = tagsResponse.Value.Tags;
        if (tags != null && tags.Count > 0)
        {
            await destBlobClient.SetTagsAsync(tags);
        }

        if (delete)
            await blobSource.DeleteIfExistsAsync();

        return destBlobClient;
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

}
