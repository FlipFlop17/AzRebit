using AzRebit.Domain.Results;
using AzRebit.Features.RebitClientStore;
using AzRebit.Shared;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.FunctionExample.Features;

/// <summary>
/// Examples on how to use Service Bus trigger with the resubmit feature
/// </summary>
public class ServiceBusCats
{
    private readonly ILogger<ServiceBusCats> _logger;
    private readonly IRebitStoreOperations _rebit;
    private bool deleteResubmitionFile = Environment.GetEnvironmentVariable("AZREBIT_DELETE_RESUBMITION_FILE") == "true";

    public ServiceBusCats(ILogger<ServiceBusCats> logger, IRebitStoreOperations rebit)
    {
        _logger = logger;
        _rebit = rebit;
    }

    /// <summary>
    /// Service Bus queue trigger example - processes messages from a Service Bus queue
    /// </summary>
    /// <param name="myQueueItem">The queue message content</param>
    /// <param name="funcContext">Function execution context</param>
    /// <returns>HTTP action result</returns>
    [Function("ProcessCatQueueMessage")]
    public async Task<IActionResult> RunQueueCat(
        [ServiceBusTrigger("cat-queue", Connection = "ServiceBusConnection")] string myQueueItem,
        FunctionContext funcContext)
    {
        _logger.LogInformation("Processing Service Bus queue message: {MessageContent}", myQueueItem);

        try
        {
            // Business logic - simulate processing
            var result = await ProcessCatMessage(myQueueItem);
            
            // Optional cleanup - delete saved files if processing was successful
            if (deleteResubmitionFile && result.IsSuccess)
            {
                await _rebit.DeleteResubmitFile(funcContext.InvocationId.ToString());
                _logger.LogInformation("Cleaned up resubmit file for successful processing");
            }

            return new OkObjectResult($"Processed queue message: {myQueueItem}");
        }
        catch (Exception ex)
        {
            // Error will be automatically captured by AzRebit middleware
            _logger.LogError(ex, "Unexpected error processing queue message: {Message}", myQueueItem);
            return new StatusCodeResult(500);
        }
    }

    /// <summary>
    /// Service Bus topic subscription trigger example
    /// </summary>
    /// <param name="message">The topic message content</param>
    /// <param name="funcContext">Function execution context</param>
    /// <returns>HTTP action result</returns>
    [Function("ProcessCatTopicMessage")]
    public async Task<IActionResult> RunTopicCat(
        [ServiceBusTrigger("cat-topic", "cat-subscription", Connection = "ServiceBusConnection")] string message,
        FunctionContext funcContext)
    {
        _logger.LogInformation("Processing Service Bus topic message: {MessageContent}", message);

        try
        {
            // Business logic - simulate processing
            var result = await ProcessCatMessage(message);
            
            // Optional cleanup
            if (deleteResubmitionFile && result.IsSuccess)
            {
                await _rebit.DeleteResubmitFile(funcContext.InvocationId.ToString());
            }

            return new OkObjectResult($"Processed topic message: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing topic message: {Message}", message);
            return new StatusCodeResult(500);
        }
    }

    /// <summary>
    /// Simulates business logic processing that might fail
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <returns>Processing result</returns>
    private async Task<RebitResult> ProcessCatMessage(string message)
    {
        // Simulate some business logic
        await Task.Delay(100); // Simulate processing time
        
        // Randomly succeed or fail for demonstration
        var random = new Random();
        if (random.Next(100) < 80) // 80% success rate
        {
            _logger.LogInformation("Successfully processed message: {Message}", message);
            return RebitResult.Success("Processing completed successfully");
        }
        else
        {
            var error = "Simulated processing error - cat data validation failed";
            _logger.LogWarning("Failed to process message: {Message}. Error: {Error}", message, error);
            return RebitResult.Failure(error);
        }
    }
}