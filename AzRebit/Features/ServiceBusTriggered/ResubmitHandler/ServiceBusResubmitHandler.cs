using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Infrastructure.ServiceBus;

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.ServiceBusTriggered.ResubmitHandler;

internal class ServiceBusResubmitHandler : IResubmitHandler
{
    private readonly IResubmitStorage _blobStorage;
    private readonly IAzureClientFactory<ServiceBusClient> _serviceBusClientFactory;
    private readonly ILogger<ServiceBusResubmitHandler> _logger;

    public TriggerType HandlerType => TriggerType.ServiceBus;

    public ServiceBusResubmitHandler(IResubmitStorage blobStorage, 
        IAzureClientFactory<ServiceBusClient> serviceBusClientFactory, 
        ILogger<ServiceBusResubmitHandler> logger)
    {
        _blobStorage = blobStorage;
        _serviceBusClientFactory = serviceBusClientFactory;
        _logger = logger;
    }

    public async Task<RebitResult<ResubmitHandlerResponse>> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        try
        {
            var storedMessage = await _blobStorage.FindAsync(invocationId);
            if (storedMessage is null)
                return RebitResult<ResubmitHandlerResponse>.Failure("ServiceBus message not found");

            var msg = await storedMessage.DownloadContentAsync();
            var jsonContent = msg.Value.Content.ToString();

            // Deserialize the ServiceBus message data
            var messageData = System.Text.Json.JsonSerializer.Deserialize<ServiceBusMessageData>(jsonContent);
            if (messageData == null)
                return RebitResult<ResubmitHandlerResponse>.Failure("Failed to deserialize ServiceBus message data");

            // Determine destination queue or topic
            string destinationName;
            bool isTopic = !string.IsNullOrEmpty(messageData.OriginalTopicName) && 
                          !string.IsNullOrEmpty(messageData.OriginalSubscriptionName);

            if (isTopic)
            {
                destinationName = messageData.OriginalTopicName!;
            }
            else
            {
                destinationName = messageData.OriginalQueueName ?? 
                                function.GetFunctionTriggerQueueName() ?? 
                                throw new InvalidOperationException("Queue name not found");
            }

            // Create ServiceBus client
            var serviceBusClient = _serviceBusClientFactory.CreateClient(function.Name);
            
            // Recreate the ServiceBus message
            var serviceBusMessage = new ServiceBusMessage(messageData.Body)
            {
                MessageId = messageData.MessageId,
                CorrelationId = messageData.CorrelationId,
                Subject = messageData.Subject,
                ContentType = messageData.ContentType
            };

            // Add custom properties
            foreach (var prop in messageData.ApplicationProperties)
            {
                serviceBusMessage.ApplicationProperties[prop.Key] = prop.Value;
            }

            foreach (var prop in messageData.UserProperties)
            {
                serviceBusMessage.ApplicationProperties[prop.Key] = prop.Value;
            }

            // Send message to destination
            if (isTopic)
            {
                // Send to topic
                var topicSender = serviceBusClient.CreateSender(destinationName);
                await topicSender.SendMessageAsync(serviceBusMessage);
            }
            else
            {
                // Send to queue
                var queueSender = serviceBusClient.CreateSender(destinationName);
                await queueSender.SendMessageAsync(serviceBusMessage);
            }

            return RebitResult<ResubmitHandlerResponse>.Success(
                new ResubmitHandlerResponse(storedMessage.Name), 
                "ServiceBus message resubmitted successfully");
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Unexpected error while resubmitting ServiceBus message");
            return RebitResult<ResubmitHandlerResponse>.Failure(e.Message);
        }
    }
}