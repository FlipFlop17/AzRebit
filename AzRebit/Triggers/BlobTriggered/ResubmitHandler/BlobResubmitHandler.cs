using System.Reflection;

using AzRebit.HelperExtensions;
using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.BlobTriggered.Middleware;
using AzRebit.Triggers.BlobTriggered.Model;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.BlobTriggered.Handler;


/// <summary>
/// The handler for blob triggered resubmissions. Does the work of copying the blob from the resubmit container to the input container
/// </summary>
internal class BlobResubmitHandler : IResubmitHandler

{
    private readonly ILogger<BlobResubmitHandler> _logger;
    private readonly IAzureClientFactory<BlobServiceClient> _blobFact;
    private BlobContainerClient _blobResubmitContainerClient;
    public TriggerName HandlerType => TriggerName.Blob;

    public BlobResubmitHandler(ILogger<BlobResubmitHandler> logger,
        IAzureClientFactory<BlobServiceClient> blobFact)
    {
        _blobResubmitContainerClient= blobFact
            .CreateClient(BlobMiddlewareHandler.BlobResubmitContainerName)
            .GetBlobContainerClient(BlobMiddlewareHandler.BlobResubmitContainerName);
        _logger = logger;
        _blobFact = blobFact;
    }

    public async Task<RebitActionResult> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        try
        {
            
            IDictionary<string, string> tags = new Dictionary<string, string>();
            function.TriggerMetadata.TryGetValue("container", out string triggerContainerName);

            BlobClient? blobForResubmitClient = await _blobResubmitContainerClient.PickUpBlobForResubmition(invocationId);
            if (blobForResubmitClient is null)
            {
                return RebitActionResult.Failure($"No blob found for invocation id {invocationId} in container {_blobResubmitContainerClient.Name}");
            }
            var existingTagsResponse = await blobForResubmitClient.GetClonedTagsAsync();
            var existingMetaResponse = await blobForResubmitClient.GetClonedMetadataAsync();

            BlobCopyFromUriOptions options = new()
            {
                Tags = existingTagsResponse,
                Metadata = existingMetaResponse
            };

            var inputBlob = CreateInputContainerClient(function.Name, triggerContainerName)
                .GetBlobClient(blobForResubmitClient.GetBlobName());

            var copyOp = await inputBlob.StartCopyFromUriAsync(blobForResubmitClient.Uri, options);

            await copyOp.WaitForCompletionAsync();

            return RebitActionResult.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Unexpected error while trying to resubmit the file {InvocationId}",invocationId);
            return RebitActionResult.Failure($"{e.Message}");
        }

    }

    private BlobContainerClient CreateInputContainerClient(string functionName , string containerName)
    {
        return _blobFact.CreateClient(functionName).GetBlobContainerClient(containerName);
    }
}
