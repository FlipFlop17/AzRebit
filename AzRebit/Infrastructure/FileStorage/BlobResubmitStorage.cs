using System.Text;

using AzRebit.Domain.Exceptions;
using AzRebit.Shared.Extensions;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace AzRebit.Infrastructure.FileStorage;

internal class BlobResubmitStorage : IResubmitStorage
{
    private int MaxTagCount = 9;

    /// <summary>
    /// Tag added to saved blob that is used as the main search attribute when
    /// </summary>
    //public const string SearchTag="InvocationId";
    public BlobContainerClient _resubmitContainerClient;

    /// <summary>
    /// Name of the container where all incoming files are saved
    /// </summary>
    public string RootSaveDirectory { get; init; }
    public string SearchTag => "InvocationId";

    public BlobResubmitStorage(IAzureClientFactory<BlobServiceClient> blobFact, IConfiguration config)
    {
        var blobContainerName = config.GetValue<string>("Rebit__ResubmitContainerName");
        if (string.IsNullOrEmpty(blobContainerName))
        {
            throw new ArgumentException("App setting Rebit__ResubmitContainerName is not defined");
        }
        RootSaveDirectory = blobContainerName;
        _resubmitContainerClient = blobFact.CreateClient(ResubmitFunctionWorkerExtension.BlobResubmitServiceClientName)
            .GetBlobContainerClient(RootSaveDirectory);
        _resubmitContainerClient.CreateIfNotExists();
    }

    public async Task<BlobClient?> FindAsync(string invocationId)
    {
        string tagFilter = $"\"{SearchTag}\" = '{invocationId}'";

        await foreach (TaggedBlobItem taggedBlob in _resubmitContainerClient.FindBlobsByTagsAsync(tagFilter))
        {
            // Return the first matching blob (should be only one)
            return _resubmitContainerClient.GetBlobClient(taggedBlob.BlobName);
        }

        return null;
    }

    /// <summary>
    /// Saves the incoming blob client/file on a dedicated storage account on the specified path
    /// </summary>
    /// <param name="sourceBlob">incoming blob</param>
    /// <param name="destinationFullPath">virtual path of the location to save the blob</param>
    /// <param name="id">Uniqueue id of the operation</param>
    /// <param name="destinationFileTags">tags to add to the blob file. File can have no more than 10 tags</param>
    /// <exception cref="BlobOperationException"></exception>
    /// <exception cref="BlobTagCountException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task SaveFileAtResubmitLocation(
        BlobBaseClient sourceBlob,
        string destinationFullPath,
        string id,
        IDictionary<string, string>? destinationFileTags)
    {
        try
        {
            var tagsToAdd = destinationFileTags ?? new Dictionary<string, string>();
            tagsToAdd.Add(SearchTag, id);
            var existingTagsResponse = await sourceBlob.GetClonedTagsAsync();
            if ((tagsToAdd.Count + existingTagsResponse.Count) > MaxTagCount)
            {
                throw new BlobTagCountException("SaveBlobAtResubmitLocation", "Invalid tag count", new Exception("Tag size reached"));
            } else
            {
                foreach (var newTag in tagsToAdd)
                {
                    // This will either add the new key or update the existing key.
                    existingTagsResponse[newTag.Key] = newTag.Value;
                }
            }

            await _resubmitContainerClient.CreateIfNotExistsAsync();
            BlobClient destinationFileClient = _resubmitContainerClient.GetBlobClient(destinationFullPath);

            BlobCopyFromUriOptions options = new BlobCopyFromUriOptions();
            options.Tags = existingTagsResponse;
            var operation = await destinationFileClient.StartCopyFromUriAsync(sourceBlob.Uri, options);
            await operation.WaitForCompletionAsync();
        }

        catch (BlobTagCountException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            throw new BlobOperationException("SaveBlobForResubmitionAsync",
             $"Failed saving blob '{sourceBlob.Name}'",
             ex);
        }
        catch (Exception ex)
        {
            throw new BlobOperationException("SaveBlobForResubmitionAsync",
            $"Unexpected failure while saving blob '{sourceBlob.Name}'",
            ex);
        }
    }

    /// <summary>
    /// Saves the incoming blob client/file on a dedicated storage account on the specified path
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="destinationFullPath">virtual path of the location to save the blob</param>
    /// <param name="id">Uniqueue id of the operation</param>
    /// <param name="destinationFileTags">tags to add to the blob file. File can have no more than 10 tags</param>
    /// <param name="encoding">defolts to UTF8 encoding</param>
    /// <exception cref="BlobOperationException"></exception>
    /// <exception cref="BlobTagCountException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task SaveFileAtResubmitLocation(
        string payload,
        string destinationFullPath,
        string id,
        IDictionary<string, string>? destinationFileTags,
        Encoding? encoding)
    {
        try
        {
            var tagsToAdd = destinationFileTags ?? new Dictionary<string, string>();
            tagsToAdd.Add(SearchTag, id);

            BlobClient blobClient = _resubmitContainerClient.GetBlobClient(destinationFullPath);
            var enc = encoding ?? Encoding.UTF8;
            using var ms = new MemoryStream(enc.GetBytes(payload));
            var options = new BlobUploadOptions();
            options.Tags = tagsToAdd;

            if (tagsToAdd.Count > MaxTagCount)
                throw new BlobTagCountException("SaveFileAtResubmitLocation", "Invalid tag count", new Exception("Tag size reached"));

            await blobClient.UploadAsync(ms, options);

        }

        catch (BlobTagCountException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            throw new BlobOperationException("SaveFileAtResubmitLocation",
             $"Failed saving blob '{destinationFullPath}'",
             ex);
        }
        catch (Exception ex)
        {
            throw new BlobOperationException("SaveBlobForResubmitionAsync",
            $"Unexpected failure while saving blob '{destinationFullPath}'",
            ex);
        }
    }

    public async Task SaveFileAtResubmitLocation(
        Stream payload,
        string destinationFullPath,
        string id,
        IDictionary<string, string>? destinationFileTags = null,
        Encoding? encoding = null)
    {
        try
        {
            var tagsToAdd = destinationFileTags ?? new Dictionary<string, string>();

            tagsToAdd.Add(SearchTag, id);
            if (tagsToAdd.Count > MaxTagCount)
                throw new BlobTagCountException("SaveFileAtResubmitLocation", "Invalid tag count", new Exception("Tag size reached"));

            BlobClient blobClient = _resubmitContainerClient.GetBlobClient(destinationFullPath);

            var options = new BlobUploadOptions
            {
                Tags = tagsToAdd,
            };
            await blobClient.UploadAsync(payload, options);
        }
        catch (BlobTagCountException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            throw new BlobOperationException("SaveFileAtResubmitLocation",
                $"Failed saving blob '{destinationFullPath}' from stream", ex);
        }
        catch (Exception ex)
        {
            throw new BlobOperationException("SaveBlobForResubmitionAsync",
                $"Unexpected failure while saving blob '{destinationFullPath}' from stream", ex);
        }
    }

    public async Task<bool> DeleteFile(string invocationId, string? searchTag = null)
    {
        try
        {
            var blobClient = await FindAsync(invocationId);

            if (blobClient is null)
            {
                throw new FileNotFoundException($"File with the tag {SearchTag}={invocationId} not found");
            }

            return await blobClient.DeleteIfExistsAsync();

        }
        catch (Exception)
        {
            throw;
        }

    }
}
