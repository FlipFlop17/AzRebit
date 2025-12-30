using Azure.Storage.Queues.Models;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.FunctionExample.Features;

internal class QueueCats
{
    private readonly ILogger<QueueCats> _logger;

    public QueueCats(ILogger<QueueCats> logger)
    {
        _logger = logger;
    }

    [Function("TransformCats-String")]
    [QueueOutput("transform-cats-output")]
    public string RunString([QueueTrigger("transform-cats-string")] string dogMessage)
    {
        _logger.LogInformation("string queueMessage: " + dogMessage);
        return dogMessage + ";has been processed";

    }
    [Function("TransformCats-QueueMessage")]
    [QueueOutput("transform-cats-output")]
    public string RunQueue([QueueTrigger("transform-cats-queuemsg")] QueueMessage dogMessage)
    {
        _logger.LogInformation("QueueMessage queueMessage: " + dogMessage.Body);
        return dogMessage.Body + ";has been processed";

    }
    [Function("TransformCats-byte")]
    [QueueOutput("transform-cats-output")]
    public string RunByte([QueueTrigger("transform-cats-byte")] byte[] dogMessage)
    {
        string decodedString = System.Text.Encoding.UTF8.GetString(dogMessage);
        _logger.LogInformation("byte queueMessage: " + decodedString);
        return decodedString + ";has been processed";

    }
    [Function("TransformCats-BinaryData")]
    [QueueOutput("transform-cats-output")]
    public string RunBinary([QueueTrigger("transform-cats-binary")] BinaryData dogMessage)
    {
        _logger.LogInformation("binary queueMessage: " + dogMessage.ToString());
        return dogMessage.ToString() + ";has been processed";

    }
    [Function("TransformCats-CustomObj")]
    [QueueOutput("transform-cats-output")]
    public string RunCustom([QueueTrigger("transform-cats-custom")] Cat cat)
    {
        _logger.LogInformation("custom queueMessage: " + cat.Name.ToString());
        return cat.Name.ToString() + ";has been processed";

    }
}
