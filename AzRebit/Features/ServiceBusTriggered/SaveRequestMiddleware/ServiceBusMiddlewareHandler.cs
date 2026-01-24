using Azure.Storage.Blobs;
using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Infrastructure.ServiceBus;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.ServiceBusTriggered.SaveRequestMiddleware;

internal class ServiceBusMiddlewareHandler : ISavePayloadHandler
{
    private readonly ILogger<ServiceBusMiddlewareHandler> _logger;
    private readonly IResubmitStorage _blobResubmit;

    internal ServiceBusMiddlewareHandler(ILogger<ServiceBusMiddlewareHandler> logger,
        IAzureClientFactory<BlobServiceClient> blobService,
        IResubmitStorage blobResubmit)
    {
        _logger = logger;
        _blobResubmit = blobResubmit;
    }

    public string ResubmitFilePrefix => "t_sb";
    public string BindingName => "serviceBusTrigger";

    public async Task<RebitResult> SaveIncomingRequest(ISavePayloadCommand command)
    {
        string invocationId = command.Context.InvocationId;
        try
        {
            var inputBindingFeature = command.Context.Features.Get<IFunctionInputBindingFeature>();
            if (inputBindingFeature is null)
            {
                return RebitResult.Failure("There are no input bindings specified");
            }

            // Extract the ServiceBus message from binding data
            command.Context.BindingContext.BindingData.TryGetValue("ServiceBusTrigger", out var serviceBusMessage);
            if (serviceBusMessage == null)
            {
                return RebitResult.Failure("ServiceBus message not found in binding data");
            }

            // Convert to ServiceBusMessageData for serialization
            var messageData = ServiceBusMessageData.FromServiceBusMessage(serviceBusMessage);

            // Extract queue/topic name from function definition for resubmission path
            var queueName = ExtractQueueName(command.Context);
            var destinationPath = queueName != null 
                ? $"{queueName}/{ResubmitFilePrefix}-{invocationId}.json"
                : $"{command.Context.FunctionDefinition.Name}/{ResubmitFilePrefix}-{invocationId}.json";

            // Serialize the message data to JSON
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(messageData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await _blobResubmit.SaveFileAtResubmitLocation(
                jsonContent,
                destinationPath,
                invocationId);

            return RebitResult.Success(invocationId);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Unexpected error while trying to save incoming ServiceBus message");
            return RebitResult.Failure(e.Message);
        }
    }

    private string? ExtractQueueName(FunctionContext context)
    {
        // For now, use function name as the queue identifier
        // This matches the pattern used by other triggers like HTTP
        return context.FunctionDefinition.Name;
    }
}