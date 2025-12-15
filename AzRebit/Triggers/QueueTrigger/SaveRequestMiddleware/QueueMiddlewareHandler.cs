using System.Data;

using AzRebit.Infrastructure;
using AzRebit.Model;
using AzRebit.Shared;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.QueueTrigger.SaveRequestMiddleware;

internal class QueueMiddlewareHandler : ISavePayloadHandler
{
    private readonly ILogger<QueueMiddlewareHandler> _logger;
    private readonly IResubmitStorage _blobResubmit;

    public string BindingName => "queueTrigger";
    public QueueMiddlewareHandler(ILogger<QueueMiddlewareHandler> logger,IAzureClientFactory<BlobServiceClient> blobService,IResubmitStorage blobResubmit)
    {
        _logger = logger;
        _blobResubmit = blobResubmit;
    }

    public async Task<RebitActionResult> SaveIncomingRequest(ISavePayloadCommand command)
    {
        string invocationId = command.Context.InvocationId;

        return RebitActionResult.Failure("not implemented");
            //var triggerProperties = context.FunctionDefinition.Parameters
            //    .Where(atr => atr.Properties.ContainsKey("bindingAttribute"))
            //    .First().Properties;

            //if (triggerProperties.TryGetValue("bindingAttribute", out var bindingAttributeObj))
            //{
            //    var bindingAttribute = bindingAttributeObj;

            //    if (bindingAttribute == null)
            //    {
            //        _logger.LogWarning("bindingAttribute is null");
            //        return ActionResult.Failure();
            //    }

            //    context.BindingContext.BindingData.TryGetValue("QueueTrigger",out var messageBody);

            //    var queueName = (bindingAttribute?.GetType().GetProperty("QueueName")
            //       ?.GetValue(bindingAttribute)?.ToString()) ?? throw new InvalidOperationException("QueueName is null in bindingAttribute");

            //    //saving the queue message to blob for resubmission
            //    BlobClient blobResubmitFile = _blobResubmitClient
            //        .GetBlobClient($"{IMiddlewareHandler.BlobPrefixForQueue}{invocationId}.txt");

            //    string messageContent = messageBody?.ToString() ?? string.Empty;
            //    BlobUploadOptions uploadOptions = new BlobUploadOptions() {
            //        Tags = new Dictionary<string, string> {
            //            { "QueueName", queueName }, { IMiddlewareHandler.BlobTagInvocationId, invocationId } }
            //    };

            //    await blobResubmitFile.UploadAsync(new BinaryData(messageContent), uploadOptions);
            //}

        }

}
