using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Domain.Exceptions;
using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Shared.Extensions;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;



namespace AzRebit.Features.BlobTriggered.ResubmitHandler;


/// <summary>
/// The handler for blob triggered resubmissions. Does the work of copying the blob from the resubmit container to the input container
/// </summary>
internal class BlobResubmitHandler : IResubmitHandler

{
    private readonly ILogger<BlobResubmitHandler> _logger;
    private readonly IAzureClientFactory<BlobServiceClient> _blobFact;
    private readonly IResubmitStorage _resubmitStorage;
    public TriggerType HandlerType => TriggerType.Blob;

    public BlobResubmitHandler(ILogger<BlobResubmitHandler> logger,
        IAzureClientFactory<BlobServiceClient> blobFact, IResubmitStorage resubmitStorage)
    {
        _logger = logger;
        _blobFact = blobFact;
        _resubmitStorage = resubmitStorage;
    }

    public async Task<RebitActionResult<ResubmitHandlerResponse>> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        try
        {
            IDictionary<string, string> tags = new Dictionary<string, string>();
            var triggerContainerName = function.GetFunctionTriggerContainerName();

            BlobClient? blobForResubmitClient = await _resubmitStorage.FindAsync(invocationId);

            if (blobForResubmitClient is null)
            {
                return RebitActionResult<ResubmitHandlerResponse>.Failure($"No blob found for invocation id {invocationId} in dedicated resubmit container", AzRebitErrorType.BlobResubmitFileNotFound);
            }
            _logger.LogResubmitWorkData(invocationId, function.Name, blobForResubmitClient.Name);
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

            return RebitActionResult<ResubmitHandlerResponse>.Success(new ResubmitHandlerResponse(blobForResubmitClient.Name));
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Unexpected error while trying to resubmit the file {InvocationId}", invocationId);
            return RebitActionResult<ResubmitHandlerResponse>.Failure(e.Message, AzRebitErrorType.UnexpectedError);
        }

    }

    private BlobContainerClient CreateInputContainerClient(string functionName, string containerName)
    {
        return _blobFact.CreateClient(functionName).GetBlobContainerClient(containerName);
    }


    private void CleanUpAnyResubmitTags(IDictionary<string, string> tags)
    {
        tags.Remove(IResubmitStorage.BlobTagInvocationId);
    }

}
