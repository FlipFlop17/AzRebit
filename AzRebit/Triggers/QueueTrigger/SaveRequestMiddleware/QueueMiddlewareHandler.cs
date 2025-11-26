using AzRebit.HelperExtensions;
using AzRebit.Shared;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.QueueTrigger.SaveRequestMiddleware;

internal class QueueMiddlewareHandler : IMiddlewareHandler
{
    private readonly ILogger<QueueMiddlewareHandler> _logger;
    private BlobContainerClient _blobResubmitClient;
    public string BindingName => "queueTrigger";
    public const string ResubmitContainerNameName = "queue-resubmits";
    public QueueMiddlewareHandler(ILogger<QueueMiddlewareHandler> logger,IAzureClientFactory<BlobServiceClient> blobService)
    {
        _logger = logger;
        _blobResubmitClient = blobService
            .CreateClient(ResubmitContainerNameName)
            .GetBlobContainerClient(ResubmitContainerNameName);
    }

    public async Task SaveIncomingRequest(FunctionContext context)
    {
        string invocationId = context.InvocationId;

        var triggerProperties = context.FunctionDefinition.Parameters
            .Where(atr => atr.Properties.ContainsKey("bindingAttribute"))
            .First().Properties;

        if (triggerProperties.TryGetValue("bindingAttribute", out var bindingAttributeObj))
        {
            var bindingAttribute = bindingAttributeObj;

            if (bindingAttribute == null)
            {
                _logger.LogWarning("bindingAttribute is null");
                return;
            }

            context.BindingContext.BindingData.TryGetValue("QueueTrigger",out var messageBody);

            var queueName = (bindingAttribute?.GetType().GetProperty("QueueName")
               ?.GetValue(bindingAttribute)?.ToString()) ?? throw new InvalidOperationException("QueueName is null in bindingAttribute");

            //saving the queue message to blob for resubmission
            BlobClient blobResubmitFile = _blobResubmitClient
                .GetBlobClient($"{IMiddlewareHandler.BlobPrefixForQueue}{invocationId}.txt");

            string messageContent = messageBody?.ToString() ?? string.Empty;
            BlobUploadOptions uploadOptions = new BlobUploadOptions() {
                Tags = new Dictionary<string, string> {
                    { "QueueName", queueName }, { IMiddlewareHandler.BlobTagInvocationId, invocationId } }
            };

            await blobResubmitFile.UploadAsync(new BinaryData(messageContent), uploadOptions);
        }

    }

}
