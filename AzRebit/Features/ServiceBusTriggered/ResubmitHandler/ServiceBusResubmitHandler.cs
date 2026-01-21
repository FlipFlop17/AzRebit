using System.Text.Json;

using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.ServiceBusTriggered.ResubmitHandler;

/// <summary>
/// Handles resubmission of Service Bus triggered functions using a "resubmit" queue pattern
/// </summary>
internal class ServiceBusResubmitHandler : IResubmitHandler
{
    private readonly ILogger<ServiceBusResubmitHandler> _logger;
    private readonly IAzureClientFactory<ServiceBusClient> _serviceBusClientFactory;
    private readonly IResubmitStorage _blobStorage;

    public TriggerType HandlerType => TriggerType.ServiceBus;
    private const string ResubmitQueueName = "azrebit-resubmit";

    internal ServiceBusResubmitHandler(
        ILogger<ServiceBusResubmitHandler> logger,
        IAzureClientFactory<ServiceBusClient> serviceBusClientFactory,
        IResubmitStorage blobStorage)
    {
        _logger = logger;
        _serviceBusClientFactory = serviceBusClientFactory;
        _blobStorage = blobStorage;
    }

    public async Task<RebitResult<ResubmitHandlerResponse>> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        try
        {
            _logger.LogInformation("Starting Service Bus resubmission for function: {FunctionName}, invocation: {InvocationId}", 
                function.Name, invocationId);

            // Load the saved minimal metadata
            var blobClient = await _blobStorage.FindAsync(invocationId);
            if (blobClient is null)
            {
                return RebitResult<ResubmitHandlerResponse>.Failure($"Failed to find metadata for resubmission: {invocationId}");
            }

            // Download the metadata
            var downloadResponse = await blobClient.DownloadContentAsync();
            var metadataJson = downloadResponse.Value.Content.ToString();
            if (string.IsNullOrEmpty(metadataJson))
            {
                return RebitResult<ResubmitHandlerResponse>.Failure("Metadata is empty");
            }

            // Parse the metadata
            var metadata = JsonSerializer.Deserialize<ResubmitMetadata>(metadataJson);
            if (metadata is null)
            {
                return RebitResult<ResubmitHandlerResponse>.Failure("Failed to parse metadata");
            }

            // Get Service Bus client
            var serviceBusClient = _serviceBusClientFactory.CreateClient(function.Name);
            
            // Send the message to the resubmit queue
            await SendToResubmitQueue(serviceBusClient, metadata, invocationId);

            _logger.LogInformation("Service Bus resubmission completed - Function: {FunctionName}, MessageId: {MessageId}, InvocationId: {InvocationId}", 
                function.Name, metadata.MessageId, invocationId);

            return RebitResult<ResubmitHandlerResponse>.Success(new ResubmitHandlerResponse(invocationId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service Bus resubmission failed - Function: {FunctionName}, InvocationId: {InvocationId}", 
                function.Name, invocationId);
            return RebitResult<ResubmitHandlerResponse>.Failure($"Service Bus resubmission failed: {ex.Message}");
        }
    }

    private async Task SendToResubmitQueue(ServiceBusClient serviceBusClient, ResubmitMetadata metadata, string invocationId)
    {
        var sender = serviceBusClient.CreateSender(ResubmitQueueName);
        
        try
        {
            // Create a new message for the resubmit queue with metadata for tracking
            var resubmitMessage = new ServiceBusMessage($"Resubmitted message for function: {metadata.FunctionName}")
            {
                MessageId = $"resubmit-{metadata.MessageId ?? Guid.NewGuid().ToString()}",
                CorrelationId = metadata.CorrelationId,
                Subject = metadata.FunctionName,
                ApplicationProperties = 
                {
                    { "OriginalInvocationId", metadata.InvocationId },
                    { "OriginalFunction", metadata.FunctionName },
                    { "OriginalEnqueuedTime", metadata.EnqueuedTime?.ToString("O") ?? "" },
                    { "ResubmittedAt", DateTime.UtcNow.ToString("O") }
                }
            };

            await sender.SendMessageAsync(resubmitMessage);
            _logger.LogInformation("Message sent to resubmit queue - Original MessageId: {MessageId}, Resubmit MessageId: {ResubmitMessageId}", 
                metadata.MessageId, resubmitMessage.MessageId);
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}

/// <summary>
/// Minimal metadata stored in blob for Service Bus resubmission tracking
/// </summary>
internal record ResubmitMetadata
{
    public string FunctionName { get; init; } = string.Empty;
    public string InvocationId { get; init; } = string.Empty;
    public string? MessageId { get; init; }
    public string? CorrelationId { get; init; }
    public DateTimeOffset? EnqueuedTime { get; init; }
    public DateTime ProcessedAt { get; init; }
}