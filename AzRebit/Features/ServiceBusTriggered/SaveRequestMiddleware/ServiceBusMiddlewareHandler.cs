using System.Text.Json;

using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Exceptions;
using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;

using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.ServiceBusTriggered.SaveRequestMiddleware;

/// <summary>
/// Middleware handler for incoming Service Bus payloads. Handles both queue messages and topic subscriptions.
/// Uses a "resubmit" queue pattern instead of blob storage for better Service Bus integration.
/// </summary>
internal class ServiceBusMiddlewareHandler : ISavePayloadHandler
{
    private readonly ILogger<ServiceBusMiddlewareHandler> _logger;
    private readonly IResubmitStorage _blobStorage;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusAdministrationClient _serviceBusAdminClient;
    private const string ResubmitQueueName = "azrebit-resubmit";

    public string BindingName => "serviceBusTrigger";
    public string ResubmitFilePrefix => "t_sbus";

    internal ServiceBusMiddlewareHandler(
        ILogger<ServiceBusMiddlewareHandler> logger, 
        IResubmitStorage blobStorage,
        ServiceBusClient serviceBusClient,
        ServiceBusAdministrationClient serviceBusAdminClient)
    {
        _logger = logger;
        _blobStorage = blobStorage;
        _serviceBusClient = serviceBusClient;
        _serviceBusAdminClient = serviceBusAdminClient;
    }

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

            var data = await inputBindingFeature.BindFunctionInputAsync(command.Context);
            
            // Extract message metadata for logging
            command.Context.BindingContext.BindingData.TryGetValue("ServiceBusTrigger", out var triggerData);
            command.Context.BindingContext.BindingData.TryGetValue("MessageId", out var messageId);
            command.Context.BindingContext.BindingData.TryGetValue("CorrelationId", out var correlationId);

            foreach (var inputData in data.Values)
            {
                switch (inputData)
                {
                    case FunctionContext context:
                        break;
                    case ServiceBusReceivedMessage serviceBusMessage:
                        // For Service Bus messages, we just store minimal info in blob (function name, invocation ID)
                        // The actual message forwarding will be handled by the resubmit handler
                        var minimalInfo = new
                        {
                            FunctionName = command.Context.FunctionDefinition.Name,
                            InvocationId = invocationId,
                            MessageId = serviceBusMessage.MessageId,
                            CorrelationId = serviceBusMessage.CorrelationId,
                            EnqueuedTime = serviceBusMessage.EnqueuedTime,
                            ProcessedAt = DateTime.UtcNow
                        };
                        
                        var minimalInfoJson = JsonSerializer.Serialize(minimalInfo, new JsonSerializerOptions { WriteIndented = true });
                        var destinationPath = $"{command.Context.FunctionDefinition.Name}/{ResubmitFilePrefix}-{messageId ?? invocationId}";
                        await _blobStorage.SaveFileAtResubmitLocation(minimalInfoJson, destinationPath, invocationId);
                        break;
                    case string payload:
                        // For simple string payloads, save directly to blob
                        var destinationPathStr = $"{command.Context.FunctionDefinition.Name}/{ResubmitFilePrefix}-{messageId ?? invocationId}";
                        await _blobStorage.SaveFileAtResubmitLocation(payload, destinationPathStr, invocationId);
                        break;
                    case byte[] payloadByte:
                        // For byte arrays, save to blob
                        using (var byteStream = new MemoryStream(payloadByte))
                        {
                            var destinationPathBytes = $"{command.Context.FunctionDefinition.Name}/{ResubmitFilePrefix}-{messageId ?? invocationId}";
                            await _blobStorage.SaveFileAtResubmitLocation(byteStream, destinationPathBytes, invocationId);
                        }
                        break;
                    default:
                        // For any other types, serialize as JSON to blob
                        var inputDataJson = JsonSerializer.Serialize(inputData, new JsonSerializerOptions { WriteIndented = true });
                        var destinationPathDefault = $"{command.Context.FunctionDefinition.Name}/{ResubmitFilePrefix}-{messageId ?? invocationId}";
                        await _blobStorage.SaveFileAtResubmitLocation(inputDataJson, destinationPathDefault, invocationId);
                        break;
                }
            }
            
            _logger.LogInformation("Service Bus function metadata saved for resubmission - Function: {FunctionName}, MessageId: {MessageId}, InvocationId: {InvocationId}", 
                command.Context.FunctionDefinition.Name, messageId, invocationId);
            
            return RebitResult.Success(invocationId);
        }
        catch (ServiceBusOperationException serviceBusE)
        {
            _logger.LogDebug(serviceBusE, "Unexpected Error on ServiceBusSavePayloadAsync() {InvocationId}", invocationId);
            return RebitResult.Failure(serviceBusE.Description);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Unexpected Error while saving Service Bus function metadata {InvocationId}", invocationId);
            return RebitResult.Failure(e.Message);
        }
    }
}

/// <summary>
/// Exception for Service Bus operations
/// </summary>
internal class ServiceBusOperationException : Exception
{
    public string Description { get; }
    public ServiceBusOperationException(string operation, string description, Exception innerException) 
        : base($"Service Bus {operation} failed: {description}", innerException)
    {
        Description = description;
    }
}