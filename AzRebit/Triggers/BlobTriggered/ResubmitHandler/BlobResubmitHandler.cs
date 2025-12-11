using AzRebit.HelperExtensions;
using AzRebit.Infrastructure;
using AzRebit.Model;
using AzRebit.Shared;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

using static AzRebit.Model.TriggerTypes;

namespace AzRebit.Triggers.BlobTriggered.Handler;


/// <summary>
/// The handler for blob triggered resubmissions. Does the work of copying the blob from the resubmit container to the input container
/// </summary>
internal class BlobResubmitHandler : IResubmitHandler

{
    private readonly ILogger<BlobResubmitHandler> _logger;
    private readonly IAzureClientFactory<BlobServiceClient> _blobFact;
    private readonly IResubmitStorage _resubmitStorage;
    public TriggerName HandlerType => TriggerName.Blob;

    public BlobResubmitHandler(ILogger<BlobResubmitHandler> logger,
        IAzureClientFactory<BlobServiceClient> blobFact,IResubmitStorage resubmitStorage)
    {
        _logger = logger;
        _blobFact = blobFact;
        _resubmitStorage = resubmitStorage;
    }

    public async Task<RebitActionResult> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        try
        {
            IDictionary<string, string> tags = new Dictionary<string, string>();
            function.TriggerMetadata.TryGetValue("container", out string? triggerContainerName);

            BlobClient? blobForResubmitClient = await _resubmitStorage.FindAsync(invocationId);
            if (blobForResubmitClient is null)
            {
                return RebitActionResult.Failure("No blob found for invocation id {invocationId} in dedicated resubmit container",AzRebitErrorType.BlobResubmitFileNotFound);
            }

            var existingTagsResponse = await blobForResubmitClient.GetClonedTagsAsync();
            var existingMetaResponse = await blobForResubmitClient.GetClonedMetadataAsync();
            CleanUpAnyResubmitTags(existingTagsResponse); //we dont want 'old' tags used for first resubmit save. we want a clean slate for retries
            
            BlobCopyFromUriOptions options = new()
            {
                Tags = existingTagsResponse,
                Metadata = existingMetaResponse
            };

            var inputBlob = CreateInputContainerClient(function.Name, triggerContainerName!)
                .GetBlobClient(Path.GetFileName(blobForResubmitClient.Name));

            var copyOp = await inputBlob.StartCopyFromUriAsync(blobForResubmitClient.Uri, options);

            await copyOp.WaitForCompletionAsync();
            //todo at this point add that the record of this resubmition to azure table
            return RebitActionResult.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Unexpected error while trying to resubmit the file {InvocationId}",invocationId);
            return RebitActionResult.Failure(e.Message,AzRebitErrorType.UnexpectedError);
        }

    }

    private BlobContainerClient CreateInputContainerClient(string functionName , string containerName)
    {
        return _blobFact.CreateClient(functionName).GetBlobContainerClient(containerName);
    }


    private void CleanUpAnyResubmitTags(IDictionary<string,string> tags)
    {
        tags.Remove(ISavePayloadsHandler.BlobTagInvocationId);
    }

}
