using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.QueueTriggered.SaveRequestMiddleware;

internal class QueueMiddlewareHandler : ISavePayloadHandler
{
    private readonly ILogger<QueueMiddlewareHandler> _logger;
    private readonly IResubmitStorage _blobResubmit;

    public QueueMiddlewareHandler(ILogger<QueueMiddlewareHandler> logger,
        IAzureClientFactory<BlobServiceClient> blobService,
        IResubmitStorage blobResubmit)
    {
        _logger = logger;
        _blobResubmit = blobResubmit;
    }

    public string ResubmitFilePrefix => "t_qu";
    public string BindingName => "queueTrigger";


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

            //queue will always expose the body no matter what type of binding is on the az function
            command.Context.BindingContext.BindingData.TryGetValue("QueueTrigger", out var messageBody);
            string messageContent = messageBody?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(messageContent))
            {
                return RebitActionResult.Success(messageContent);
            }
            var destinationPath = $"{command.Context.FunctionDefinition.Name}/{ResubmitFilePrefix}-{invocationId}.txt";
            await _blobResubmit.SaveFileAtResubmitLocation(
                      messageContent,
                      destinationPath,
                      invocationId
                      );
            return RebitActionResult.Success(invocationId);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Unexpected erorr while trying to save incoming QueueMessage");
            return RebitActionResult.Failure(e.Message);
        }

    }

}
