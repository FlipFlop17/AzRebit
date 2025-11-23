using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzRebit.HelperExtensions;
using AzRebit.Shared;

using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;

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
            // Extract connection from attribute
            var connectionProperty = bindingAttribute?.GetType().GetProperty("Connection")
                ?.GetValue(bindingAttribute)?.ToString();

            var queueName = bindingAttribute?.GetType().GetProperty("QueueName")
               ?.GetValue(bindingAttribute)?.ToString();

            //saving the queue message to blob for resubmission
            BlobClient blobResubmitFile= _blobResubmitClient.GetBlobClient(queueName);
            await blobResubmitFile.SaveBlobForResubmitionAsync(invocationId);
            //try
            //{
            //    // Create QueueClient using appropriate authentication method
            //    BlobClient blobClient = await CreateBlobClientAsync(connectionProperty, sourceContainerProperty, sourceBlobNameProperty);

            //    if (blobClient != null)
            //    {
            //        await blobClient.SaveBlobForResubmitionAsync(invocationId);
            //        _logger.LogInformation("Blob {BlobName} saved for resubmission with invocationId {InvocationId}",
            //            sourceBlobNameProperty, invocationId);
            //    } else
            //    {
            //        _logger.LogError("Failed to create BlobClient for blob {BlobName}", sourceBlobNameProperty);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error saving blob {BlobName} for resubmission", sourceBlobNameProperty);
            //    throw;
            //}
        }

    }

}
