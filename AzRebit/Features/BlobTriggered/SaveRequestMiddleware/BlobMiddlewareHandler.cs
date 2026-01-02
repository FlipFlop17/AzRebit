using System.Text.Json;

using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Exceptions;
using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Shared.Extensions;

using Azure.Storage.Blobs.Specialized;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.BlobTriggered.SaveRequestMiddleware;


/// <summary>
/// Middleware handler for incoming blob payloads. Depending on blob triggered params it saves the blob for resubmission.
/// </summary>
internal class BlobMiddlewareHandler : ISavePayloadHandler
{
    private readonly ILogger<BlobMiddlewareHandler> _logger;
    private readonly IResubmitStorage _blobStorage;
    public string BindingName => "blobTrigger";
    public string ResubmitFilePrefix => "t_blob";
    internal BlobMiddlewareHandler(ILogger<BlobMiddlewareHandler> logger, IResubmitStorage blobStorage)
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
            command.Context.BindingContext.BindingData.TryGetValue("BlobTrigger", out var blobPath);
            if (blobPath is null)
                throw new BlobOperationException("BindingData.TryGetValue", "Cannot find blob name", new Exception());

            var blobName = Path.GetFileName(blobPath.ToString());
            string destinationPath = $"{command.Context.FunctionDefinition.Name}/{ResubmitFilePrefix}-{blobName}";

            foreach (var inputData in data.Values)
            {
                switch (inputData)
                {
                    case FunctionContext context:
                        break;
                    case string payload:
                        await _blobStorage.SaveFileAtResubmitLocation(payload, destinationPath, invocationId);
                        break;
                    case byte[] payloadByte:
                        using (Stream byteStream = new MemoryStream(payloadByte))
                        {
                            await _blobStorage.SaveFileAtResubmitLocation(byteStream, destinationPath, invocationId);
                        }
                        ;
                        break;
                    case Stream payloadStream:
                        await _blobStorage.SaveFileAtResubmitLocation(payloadStream, destinationPath, invocationId);
                        if (payloadStream.CanSeek)
                            payloadStream.Seek(0, SeekOrigin.Begin);

                        payloadStream.Position = 0;
                        break;
                    case BlobBaseClient blobClientBase:
                        await _blobStorage.SaveFileAtResubmitLocation(blobClientBase, destinationPath, invocationId);
                        break;
                    default:
                        string serializedPayload = JsonSerializer.Serialize(inputData); //can be expensive for RAM - user should be using blobclient or stream
                        await _blobStorage.SaveFileAtResubmitLocation(serializedPayload, destinationPath, invocationId);
                        break;
                }
            }
            return RebitActionResult.Success(invocationId);

        }
        catch (BlobOperationException blobE)
        {
            _logger.LogDebug(blobE, "Unexpected Error on SaveBlobForResubmitionAsync() {InvocationId}", invocationId);
            return RebitActionResult.Failure(blobE.Description);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Unexpected Error while saving incoming request {InvocationId}", invocationId);
            return RebitActionResult.Failure(e.Message);
        }

    }
}
