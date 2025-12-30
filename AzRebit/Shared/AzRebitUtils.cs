using AzRebit.Infrastructure.FileStorage;

using Azure.Storage.Blobs;

namespace AzRebit.Shared;

/// <summary>
/// Provides utility methods for managing resubmission blobs in Azure Blob Storage for Azure Functions.
/// </summary>
/// <remarks>This static class contains helper methods intended for use with Azure Functions that require
/// interaction with the resubmission container in Azure Blob Storage. All members are thread-safe and can be used
/// concurrently across multiple function executions.</remarks>
public static class AzRebitUtils
{
    /// <summary>
    /// Deletes a saved blob from the resubmission container
    /// </summary>
    /// <param name="invocationId">The uniqueue id of the execution. Usually fetched from <c>FunctionContext.InvocationId</c></param>
    /// <returns></returns>
    public static async Task<bool> DeleteSavedResubmitionBlobAsync(string invocationId)
    {
        var containerClient = new BlobContainerClient(
            Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
            BlobResubmitStorage.ResubmitContainerName);

        if (!await containerClient.ExistsAsync())
        {
            return false;
        }

        var blobClient = containerClient.GetBlobClient(invocationId);
        return await blobClient.DeleteIfExistsAsync();
    }
}
