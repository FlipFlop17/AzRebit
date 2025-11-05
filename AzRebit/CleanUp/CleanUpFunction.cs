using AzRebit.Shared;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static AzRebit.ResubmitExtension;
using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.CleanUp;

/// <summary>
/// runs on a timer to clean up old resubmit requests
/// </summary>
internal class CleanUpFunction 
{
    private readonly IOptions<ResubmitOptions> _options;
    private readonly ILogger<CleanUpFunction> _logger;
    private readonly IEnumerable<ITriggerHandler> _triggerHandlers;

    public CleanUpFunction(
        IOptions<ResubmitOptions> options,
        ILogger<CleanUpFunction> logger,
         IEnumerable<ITriggerHandler> triggerHandlers)
    {
        _options = options;
        _logger = logger;
        _triggerHandlers = triggerHandlers;
    }

    /// <summary>
    /// Timer trigger that runs cleanup daily at 01 UTC.
    /// </summary>
    [Function("CleanupSavedResubmits")]
    public async Task RunCleanup(
        [TimerTrigger("0 0 1 * * *")] TimerInfo timerInfo,
        FunctionContext context)
    {
        _logger.LogInformation(
            "Cleanup timer function started at {Time}. Next run: {NextRun}",
            DateTime.UtcNow,
            timerInfo.ScheduleStatus?.Next);

        try
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;

            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-_options.Value.DaysToKeepRequests);
            _logger.LogInformation(
                "Starting cleanup of blobs older than {CutoffDate} (keeping last {Days} days)",
                cutoffDate,
                _options.Value.DaysToKeepRequests);

            var blobServiceClient = new BlobServiceClient(connectionString);

            // Cleanup all trigger type containers
            await CleanupAllContainersAsync(blobServiceClient, cutoffDate, context.CancellationToken);

            _logger.LogInformation("Cleanup completed successfully at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during cleanup process");
            throw; // Re-throw to mark function execution as failed
        }
    }

    private async Task CleanupAllContainersAsync(
        BlobServiceClient blobServiceClient,
        DateTimeOffset cutoffDate,
        CancellationToken cancellationToken)
    {
        var totalDeleted = 0;
        var totalErrors = 0;

        // Loop through all trigger types and clean their containers
        foreach (var handler in _triggerHandlers)
        {
            var containerName = handler.ContainerName;
            var triggerType = handler.HandlerType;
            _logger.LogInformation("Cleaning up container for {TriggerType} trigger: {ContainerName}",
                triggerType, containerName);

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                _logger.LogDebug("Container {ContainerName} does not exist. Skipping.", containerName);
                continue;
            }

            var deletedCount = 0;
            var errorCount = 0;

            await foreach (var blobItem in containerClient.GetBlobsAsync(
                traits: BlobTraits.Metadata,
                cancellationToken: cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Cleanup cancelled for container {ContainerName}", containerName);
                    break;
                }

                // Check if blob is older than cutoff date
                if (blobItem.Properties.CreatedOn.HasValue &&
                    blobItem.Properties.CreatedOn.Value < cutoffDate)
                {
                    try
                    {
                        var blobClient = containerClient.GetBlobClient(blobItem.Name);
                        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                        deletedCount++;

                        _logger.LogDebug(
                            "Deleted old blob: {BlobName} from {TriggerType} container (Created: {CreatedOn})",
                            blobItem.Name,
                            triggerType,
                            blobItem.Properties.CreatedOn);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogWarning(
                            ex,
                            "Failed to delete blob: {BlobName} from container {ContainerName}",
                            blobItem.Name,
                            containerName);
                    }
                }
            }

            totalDeleted += deletedCount;
            totalErrors += errorCount;

            _logger.LogInformation(
                "Cleanup summary for {TriggerType} ({ContainerName}): Deleted {DeletedCount} blobs, {ErrorCount} errors",
                triggerType,
                containerName,
                deletedCount,
                errorCount);
        }

        _logger.LogInformation(
            "Total cleanup summary: Deleted {TotalDeleted} blobs across all containers, {TotalErrors} errors",
            totalDeleted,
            totalErrors);
    }

}
