using AzRebit.Infrastructure;
using AzRebit.Model;
using AzRebit.Model.Exceptions;
using AzRebit.Shared;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.BlobTriggered.Middleware;


/// <summary>
/// Middleware handler for incoming blob payloads. Depending on blob triggered params it saves the blob for resubmission.
/// </summary>
public class BlobMiddlewareHandler : ISavePayloadHandler
{
    private readonly ILogger<BlobMiddlewareHandler> _logger;
    private readonly IResubmitStorage _blobStorage;
    public string BindingName => "blobTrigger";

    public BlobMiddlewareHandler(ILogger<BlobMiddlewareHandler> logger,IResubmitStorage blobStorage)
    {
        _logger = logger;
        _blobStorage = blobStorage;
    }

    public async Task<RebitActionResult> SaveIncomingRequest(ISavePayloadCommand command)
    {
        string invocationId = command.Context.InvocationId;
        try
        {
            var inputBindingFeature = command.Context.Features.Get<IFunctionInputBindingFeature>();
            if (inputBindingFeature is null)
            {
                return RebitActionResult.Failure("There is not input bindings specified");
            }

            var data = await inputBindingFeature.BindFunctionInputAsync(command.Context);
            var inputData = data.Values;

            var blobClient = inputData.OfType<BlobClient>().FirstOrDefault();
            if (blobClient != null)
            {
                var destinationPath=$"{command.Context.FunctionDefinition.Name}/{blobClient.Name}";
                await _blobStorage.SaveFileAtResubmitLocation(
                    blobClient, 
                    destinationPath,
                    new Dictionary<string,string>() { { IResubmitStorage.BlobTagInvocationId,invocationId } }
                    );
                return RebitActionResult.Success(invocationId);
            }
            return RebitActionResult.Failure("Blob Client not found");
        }
        catch (BlobOperationException blobE)
        {
            _logger.LogError(blobE, "Unexpected Error on SaveBlobForResubmitionAsync() {InvocationId}", invocationId);
            return RebitActionResult.Failure(blobE.Description);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected Error while saving incoming request {InvocationId}", invocationId);
            return RebitActionResult.Failure(e.Message);
        }

    }
}
