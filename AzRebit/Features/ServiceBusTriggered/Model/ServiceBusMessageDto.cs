using System.Text.Json;

using AzRebit.Domain.Entities;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;

namespace AzRebit.Features.ServiceBusTriggered.Model;

/// <summary>
/// Data transfer object for Service Bus messages
/// </summary>
internal record ServiceBusMessageDto
{
    public string? Body { get; init; }
    public string? ContentType { get; init; }
    public string? MessageId { get; init; }
    public string? CorrelationId { get; init; }
    public string? SessionId { get; init; }
    public string? ReplyTo { get; init; }
    public string? ReplyToSessionId { get; init; }
    public string? Subject { get; init; }
    public DateTimeOffset? ScheduledEnqueueTimeUtc { get; init; }
    public DateTimeOffset? EnqueuedTimeUtc { get; init; }
    public long? SequenceNumber { get; init; }
    public Dictionary<string, object>? Properties { get; init; }
    public Dictionary<string, object>? ApplicationProperties { get; init; }
    public string? DeadLetterReason { get; init; }
    public string? DeadLetterErrorDescription { get; init; }

    public static ServiceBusMessageDto FromServiceBusMessage(ServiceBusReceivedMessage message)
    {
        return new ServiceBusMessageDto
        {
            Body = message.Body?.ToString(),
            ContentType = message.ContentType,
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            SessionId = message.SessionId,
            ReplyTo = message.ReplyTo,
            ReplyToSessionId = message.ReplyToSessionId,
            Subject = message.Subject,
            ScheduledEnqueueTimeUtc = message.ScheduledEnqueueTime,
            EnqueuedTimeUtc = message.EnqueuedTime,
            SequenceNumber = message.SequenceNumber,
            Properties = message.ApplicationProperties?.ToDictionary(x => x.Key, x => (object)x.Value),
            ApplicationProperties = message.ApplicationProperties?.ToDictionary(x => x.Key, x => (object)x.Value),
            DeadLetterReason = message.DeadLetterReason,
            DeadLetterErrorDescription = message.DeadLetterErrorDescription
        };
    }

    public string Serialize() => JsonSerializer.Serialize(this);
}