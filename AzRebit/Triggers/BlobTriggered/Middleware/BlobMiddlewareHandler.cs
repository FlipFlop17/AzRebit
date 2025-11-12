using AzRebit.HelperExtensions;
using AzRebit.Shared;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.BlobTriggered.Middleware;


/// <summary>
/// Middleware handler for incoming blob payloads. Depending on blob triggered params it saves the blob for resubmission.
/// </summary>
internal class BlobMiddlewareHandler:IMiddlewareHandler
{
    private readonly ILogger<BlobMiddlewareHandler> _logger;

    public BlobMiddlewareHandler(ILogger<BlobMiddlewareHandler> logger)
    {
        _logger = logger;
    }

    public string BindingName => "blobTrigger";

    public async Task SaveIncomingRequest(FunctionContext context)
    {

        BlobBaseClient? incomingBlobClient = null;
        string? blobName = null;
        string? connectionName = null;

        // Try to extract blob client from binding data
        // Order matters: try most specific types first, then fall back to base types
        var bindingValues = context.BindingContext.BindingData.Values;

        // Check for specialized blob client types (all inherit from BlobBaseClient)
        var appendBlobClient = bindingValues.OfType<AppendBlobClient>().FirstOrDefault();
        if (appendBlobClient != null)
        {
            incomingBlobClient = appendBlobClient;
            blobName = appendBlobClient.Name;
            _logger.LogDebug("Found AppendBlobClient binding");
        }

        if (incomingBlobClient == null)
        {
            var pageBlobClient = bindingValues.OfType<PageBlobClient>().FirstOrDefault();
            if (pageBlobClient != null)
            {
                incomingBlobClient = pageBlobClient;
                blobName = pageBlobClient.Name;
                _logger.LogDebug("Found PageBlobClient binding");
            }
        }

        if (incomingBlobClient == null)
        {
            var blockBlobClient = bindingValues.OfType<BlockBlobClient>().FirstOrDefault();
            if (blockBlobClient != null)
            {
                incomingBlobClient = blockBlobClient;
                blobName = blockBlobClient.Name;
                _logger.LogDebug("Found BlockBlobClient binding");
            }
        }

        if (incomingBlobClient == null)
        {
            var blobBaseClient = bindingValues.OfType<BlobBaseClient>().FirstOrDefault();
            if (blobBaseClient != null)
            {
                incomingBlobClient = blobBaseClient;
                blobName = blobBaseClient.Name;
                _logger.LogDebug("Found BlobBaseClient binding");
            }
        }

        if (incomingBlobClient == null)
        {
            // Fall back to standard BlobClient
            incomingBlobClient = bindingValues.OfType<BlobClient>().FirstOrDefault();
            if (incomingBlobClient != null)
            {
                blobName = incomingBlobClient.Name;
                _logger.LogDebug("Found BlobClient binding");
            }
        }

        string invocationId = context.InvocationId;

        // If we found a blob client, use the existing method
        if (incomingBlobClient != null && !string.IsNullOrEmpty(blobName))
        {
            _logger.LogInformation(
                "Auto-saving blob {BlobName} for resubmission with invocationId: {invocationId}",
                blobName,
                invocationId);

            await incomingBlobClient.SaveBlobForResubmitionAsync(invocationId);
        }
        // Handle primitive types (string, byte[], Stream, POCO)
        else
        {
            // Try to get blob name from trigger metadata
            if (context.BindingContext.BindingData.TryGetValue("BlobTrigger", out var blobTriggerValue))
            {
                blobName = blobTriggerValue?.ToString();

                if (!string.IsNullOrEmpty(blobName))
                {
                    // Try to get the actual content from binding data
                    var contentSaved = false;

                    // Check for Stream
                    var streamContent = bindingValues.OfType<Stream>().FirstOrDefault();
                    if (streamContent != null)
                    {
                        _logger.LogInformation(
                            "Auto-saving blob content (Stream) {BlobName} for resubmission with invocationId: {invocationId}",
                            blobName,
                            invocationId);

                        await AzRebitBlobExtensions.SaveBlobContentForResubmissionAsync(streamContent, blobName, invocationId);
                        contentSaved = true;
                    }

                    // Check for byte[]
                    if (!contentSaved)
                    {
                        var byteContent = bindingValues.OfType<byte[]>().FirstOrDefault();
                        if (byteContent != null)
                        {
                            _logger.LogInformation(
                                "Auto-saving blob content (byte[]) {BlobName} for resubmission with invocationId: {invocationId}",
                                blobName,
                                invocationId);

                            await AzRebitBlobExtensions.SaveBlobContentForResubmissionAsync(byteContent, blobName, invocationId);
                            contentSaved = true;
                        }
                    }

                    // Check for string
                    if (!contentSaved)
                    {
                        var stringContent = bindingValues.OfType<string>().FirstOrDefault();
                        if (stringContent != null)
                        {
                            _logger.LogInformation(
                                "Auto-saving blob content (string) {BlobName} for resubmission with invocationId: {invocationId}",
                                blobName,
                                invocationId);

                            await AzRebitBlobExtensions.SaveBlobContentForResubmissionAsync(stringContent, blobName, invocationId);
                            contentSaved = true;
                        }
                    }

                    if (!contentSaved)
                    {
                        _logger.LogWarning(
                            "Blob trigger detected with primitive type binding but unable to extract content in function {FunctionName}",
                            context.FunctionDefinition.Name);
                    }
                }
            } else
            {
                _logger.LogWarning(
                    "Blob trigger detected but unable to extract blob information in function {FunctionName}",
                    context.FunctionDefinition.Name);
            }
        }
        
    }

}
