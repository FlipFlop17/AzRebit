using System.Text.Json;

namespace AzRebit.Infrastructure.ServiceBus;

/// <summary>
/// Represents a ServiceBus message for serialization to blob storage
/// </summary>
public class ServiceBusMessageData
{
    /// <summary>
    /// The message body content
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// The unique identifier for the message
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// The correlation identifier
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The subject or label of the message
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The content type of the message
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The time when the message was enqueued
    /// </summary>
    public DateTimeOffset? EnqueuedTime { get; set; }

    /// <summary>
    /// Application properties attached to the message
    /// </summary>
    public Dictionary<string, object> ApplicationProperties { get; set; } = new();

    /// <summary>
    /// User properties attached to the message
    /// </summary>
    public Dictionary<string, object> UserProperties { get; set; } = new();

    /// <summary>
    /// The original queue name (for queue triggers)
    /// </summary>
    public string? OriginalQueueName { get; set; }

    /// <summary>
    /// The original topic name (for topic triggers)
    /// </summary>
    public string? OriginalTopicName { get; set; }

    /// <summary>
    /// The original subscription name (for topic triggers)
    /// </summary>
    public string? OriginalSubscriptionName { get; set; }

    /// <summary>
    /// Creates a ServiceBusMessageData from a ServiceBusReceivedMessage
    /// </summary>
    /// <param name="message">The ServiceBus message</param>
    /// <returns>A ServiceBusMessageData instance</returns>
    public static ServiceBusMessageData FromServiceBusMessage(object message)
    {
        var data = new ServiceBusMessageData();

        try
        {
            // Extract body as string
            if (message != null)
            {
                var bodyProperty = message.GetType().GetProperty("Body");
                if (bodyProperty != null)
                {
                    var body = bodyProperty.GetValue(message);
                    if (body != null)
                    {
                        // Handle different body types
                        if (body is string stringBody)
                        {
                            data.Body = stringBody;
                        }
                        else if (body is byte[] bytesBody)
                        {
                            data.Body = Convert.ToBase64String(bytesBody);
                        }
                        else
                        {
                            // Try to serialize to JSON
                            data.Body = JsonSerializer.Serialize(body);
                        }
                    }
                }

                // Extract other properties
                data.MessageId = GetPropertyValue<string>(message, "MessageId");
                data.CorrelationId = GetPropertyValue<string>(message, "CorrelationId");
                data.Subject = GetPropertyValue<string>(message, "Subject");
                data.ContentType = GetPropertyValue<string>(message, "ContentType");
                data.EnqueuedTime = GetPropertyValue<DateTimeOffset?>(message, "EnqueuedTime");

                // Extract application properties
                var applicationPropertiesProperty = message.GetType().GetProperty("ApplicationProperties");
                if (applicationPropertiesProperty != null)
                {
                    var applicationProperties = applicationPropertiesProperty.GetValue(message);
                    if (applicationProperties is IDictionary<string, object> dict)
                    {
                        data.ApplicationProperties = new Dictionary<string, object>(dict);
                    }
                }

                // Extract user properties  
                var userPropertiesProperty = message.GetType().GetProperty("UserProperties");
                if (userPropertiesProperty != null)
                {
                    var userProperties = userPropertiesProperty.GetValue(message);
                    if (userProperties is IDictionary<string, object> dict)
                    {
                        data.UserProperties = new Dictionary<string, object>(dict);
                    }
                }
            }
        }
        catch (Exception)
        {
            // If extraction fails, we still want to save what we can
        }

        return data;
    }

    private static T? GetPropertyValue<T>(object obj, string propertyName)
    {
        try
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property != null ? (T?)property.GetValue(obj) : default;
        }
        catch
        {
            return default;
        }
    }
}