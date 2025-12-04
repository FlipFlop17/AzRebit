
using System.ClientModel.Primitives;

using AzRebit.HelperExtensions;
using AzRebit.Shared;
using AzRebit.Shared.Exceptions;
using AzRebit.Shared.Model;

using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

using Grpc.Net.Client.Configuration;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.BlobTriggered.Middleware;

/// <summary>
/// Middleware handler for incoming blob payloads. Depending on blob triggered params it saves the blob for resubmission.
/// </summary>
public class BlobMiddlewareHandler : IMiddlewareHandler
{
    private readonly ILogger<BlobMiddlewareHandler> _logger;
    private readonly BlobContainerClient _blobResubmitClient;

    /// <summary>
    /// the name of the container where blobs for resubmition are stored
    /// </summary>
    public const string BlobResubmitContainerName = "blob-resubmits";
    public string BindingName => "blobTrigger";

    public BlobMiddlewareHandler(ILogger<BlobMiddlewareHandler> logger,IAzureClientFactory<BlobServiceClient> blobFact)
    {
        _logger = logger;
        _blobResubmitClient = blobFact
           .CreateClient(BlobResubmitContainerName)
           .GetBlobContainerClient(BlobResubmitContainerName);
    }


    public async Task<RebitActionResult> SaveIncomingRequest(FunctionContext context)
    {
        string invocationId = context.InvocationId;
        try
        {
            var inputBindingFeature = context.Features.Get<IFunctionInputBindingFeature>();
            if (inputBindingFeature is null)
            {
                return RebitActionResult.Failure("There is not input bindings specified");
            }

            var data = await inputBindingFeature.BindFunctionInputAsync(context);
            var inputData = data.Values;

            var blobClient = inputData.OfType<BlobClient>().FirstOrDefault();
            if (blobClient != null)
            {
                await SaveBlobForResubmitionAsync(blobClient, invocationId);
                return RebitActionResult.Success();
            }
            return RebitActionResult.Failure("Blob Client not found");
        }
        catch (BlobOperationException blobE)
        {
            _logger.LogError(blobE, "Unexpected Error on SaveBlobForResubmitionAsync() {InvocationId}", invocationId);
            return RebitActionResult.Failure(blobE.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected Error while saving incoming request {InvocationId}", invocationId);
            return RebitActionResult.Failure(e.Message);
        }

    }
    private async Task SaveBlobForResubmitionAsync(BlobClient sourceblobClient, string id)
    {
        try
        {
            var existingTagsResponse = await sourceblobClient.GetClonedTagsAsync();

            var destinationBlobName = $"{id}{Path.GetExtension(sourceblobClient.Name)}";

            var saveContainer = _blobResubmitClient;

            await saveContainer.CreateIfNotExistsAsync();
            BlobClient destinationFileClient = saveContainer.GetBlobClient(destinationBlobName);
            var operation = await destinationFileClient.StartCopyFromUriAsync(sourceblobClient.Uri);
            await operation.WaitForCompletionAsync();
            
            //set tags
            if(existingTagsResponse.Count<10)
                existingTagsResponse[IMiddlewareHandler.BlobTagInvocationId] = id;
            
            await destinationFileClient.SetTagsAsync(existingTagsResponse);
        }
        catch (RequestFailedException ex)
        {
            throw new BlobOperationException("SaveBlobForResubmitionAsync",
             $"Failed saving blob '{sourceblobClient.Name}'",
             ex);
        }
        catch (Exception ex)
        {
            throw new BlobOperationException("SaveBlobForResubmitionAsync",
            $"Unexpected failure while saving blob '{sourceblobClient.Name}'",
            ex);
        }

    }
}
