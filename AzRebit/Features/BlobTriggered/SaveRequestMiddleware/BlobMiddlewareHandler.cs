using System.Text.Json;

using AzRebit.Infrastructure.FileStorage;
using AzRebit.Domain.Exceptions;

using Azure.Storage.Blobs.Specialized;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Logging;
using AzRebit.Domain.Results;
using AzRebit.Domain.Abstractions;

namespace AzRebit.Features.BlobTriggered.SaveRequestMiddleware;


/// <summary>
/// Middleware handler for incoming blob payloads. Depending on blob triggered params it saves the blob for resubmission.
/// </summary>
public class BlobMiddlewareHandler : ISavePayloadHandler
{
    private readonly ILogger<BlobMiddlewareHandler> _logger;
    private readonly IResubmitStorage _blobStorage;
    public string BindingName => "blobTrigger";

    private const string _prefix = "t_blob";
    public static string ResubmitFilePrefix => _prefix;
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
            command.Context.BindingContext.BindingData.TryGetValue("BlobTrigger", out var blobPath);
            if (blobPath is null)
                throw new BlobOperationException("BindingData.TryGetValue", "Cannot find blob name",new Exception());

            string blobName = Path.GetFileName(blobPath.ToString());
            string destinationPath = $"{command.Context.FunctionDefinition.Name}/{_prefix}-{blobName}";
            var resubmitTag = new Dictionary<string, string>() { { IResubmitStorage.BlobTagInvocationId, invocationId } };

            foreach (var inputData in data.Values)
            {
                switch (inputData)
                {
                    case FunctionContext context:
                        break;
                    case string payload:
                        await _blobStorage.SaveFileAtResubmitLocation(payload, destinationPath, resubmitTag);
                        break;
                    case byte[] payloadByte:
                        using (Stream byteStream = new MemoryStream(payloadByte))
                        {
                            await _blobStorage.SaveFileAtResubmitLocation(byteStream, destinationPath, resubmitTag);
                        };
                        break;
                    case Stream payloadStream:
                        await _blobStorage.SaveFileAtResubmitLocation(payloadStream, destinationPath, resubmitTag);
                        if(payloadStream.CanSeek)
                            payloadStream.Seek(0, SeekOrigin.Begin);

                        payloadStream.Position = 0;
                        break;
                    case BlobBaseClient blobClientBase:
                        await _blobStorage.SaveFileAtResubmitLocation(blobClientBase, destinationPath, resubmitTag);
                        break;
                    default:
                        string serializedPayload = JsonSerializer.Serialize(inputData); //can be expensive for RAM - user should be using blobclient or stream
                        await _blobStorage.SaveFileAtResubmitLocation(serializedPayload, destinationPath, resubmitTag);
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
