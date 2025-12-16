using System;

using AzRebit.Infrastructure;
using AzRebit.Model;
using AzRebit.Shared;

using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.QueueTrigger.SaveRequestMiddleware;

internal class QueueMiddlewareHandler : ISavePayloadHandler
{
    private readonly ILogger<QueueMiddlewareHandler> _logger;
    private readonly IResubmitStorage _blobResubmit;
    private const string _prefix = "queue";
    public static string ResubmitFilePrefix => _prefix;
    public string BindingName => "queueTrigger";
    public QueueMiddlewareHandler(ILogger<QueueMiddlewareHandler> logger,IAzureClientFactory<BlobServiceClient> blobService,IResubmitStorage blobResubmit)
    {
        _logger = logger;
        _blobResubmit = blobResubmit;
    }

    public async Task<RebitActionResult> SaveIncomingRequest(ISavePayloadCommand command)
    {
        string invocationId = command.Context.InvocationId;

        var inputBindingFeature = command.Context.Features.Get<IFunctionInputBindingFeature>();
        if (inputBindingFeature is null)
        {
            return RebitActionResult.Failure("There is not input bindings specified");
        }

        //queue will always expose the body no matter what type of binding is on the az function
        command.Context.BindingContext.BindingData.TryGetValue("QueueTrigger", out var messageBody);
        string messageContent = messageBody?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(messageContent))
        {
            return RebitActionResult.Success(messageContent);
        }
        var destinationPath = $"{command.Context.FunctionDefinition.Name}/{_prefix}-{invocationId}.txt";
        await _blobResubmit.SaveFileAtResubmitLocation(
                  messageContent,
                  destinationPath,
                  new Dictionary<string, string>() { { IResubmitStorage.BlobTagInvocationId, invocationId } }
                  );
        return RebitActionResult.Success(invocationId);

    }

}
