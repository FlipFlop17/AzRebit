using System.Text;

using AzRebit.HelperExtensions;
using AzRebit.Model.Exceptions;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Azure;

namespace AzRebit.Infrastructure;

internal class BlobResubmitStorage : IResubmitStorage
{
    private int  MaxTagCount= 10;
    public BlobContainerClient _resubmitContainerClient;
    
    /// <summary>
    /// Name of the container where all incoming files are saved
    /// </summary>
    public static string ResubmitContainerName => "files-for-resubmit";
    public BlobResubmitStorage(IAzureClientFactory<BlobServiceClient> blobFact)
    {
        _resubmitContainerClient = blobFact.CreateClient(ResubmitFunctionWorkerExtension.BlobResubmitServiceClientName)
            .GetBlobContainerClient(ResubmitContainerName);
    }


    public async Task<BlobClient?> FindAsync(string invocationId)
    {
        string tagFilter = $"\"{IResubmitStorage.BlobTagInvocationId}\" = '{invocationId}'";

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
    /// <param name="destinationFileTags">tags to add to the blob file. File can have no more than 10 tags</param>
    /// <exception cref="BlobOperationException"></exception>
    /// <exception cref="BlobTagCountException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task SaveFileAtResubmitLocation(BlobClient sourceBlob, string destinationFullPath,IDictionary<string,string>? destinationFileTags)
    {
        try
        {
            var tagsToAdd=destinationFileTags ?? new Dictionary<string, string>();
            var existingTagsResponse = await sourceBlob.GetClonedTagsAsync();
            if ((tagsToAdd.Count+existingTagsResponse.Count)>MaxTagCount)
            {
                throw new BlobTagCountException("SaveBlobAtResubmitLocation", "Check tag count", new Exception("Tag count on a blob cannot be more than 10"));
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
    /// <param name="sourceBlob">incoming blob</param>
    /// <param name="destinationFullPath">virtual path of the location to save the blob</param>
    /// <param name="destinationFileTags">tags to add to the blob file. File can have no more than 10 tags</param>
    /// <param name="encoding">defolts to UTF8 encoding</param>
    /// <exception cref="BlobOperationException"></exception>
    /// <exception cref="BlobTagCountException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task SaveFileAtResubmitLocation(string payload, string destinationFullPath, IDictionary<string, string>? destinationFileTags, Encoding? encoding)
    {
        try
        {
            var tagsToAdd=destinationFileTags ?? new Dictionary<string, string>();
            BlobClient blobClient = _resubmitContainerClient.GetBlobClient(destinationFullPath);
            var enc=encoding ?? Encoding.UTF8;
            using var ms = new MemoryStream(enc.GetBytes(payload));
            var options = new BlobUploadOptions();
            options.Tags = tagsToAdd;

            if(tagsToAdd.Count>MaxTagCount)
                throw new BlobTagCountException("SaveFileAtResubmitLocation", "Invalid tag count",new Exception("Tag size reached"));

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


}
