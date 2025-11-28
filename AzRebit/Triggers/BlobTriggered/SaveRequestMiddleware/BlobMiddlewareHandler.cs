using AzRebit.HelperExtensions;
using AzRebit.Shared;

using Azure.Identity;
using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.BlobTriggered.Middleware;



/// <summary>
/// Middleware handler for incoming blob payloads. Depending on blob triggered params it saves the blob for resubmission.
/// </summary>
internal class BlobMiddlewareHandler : IMiddlewareHandler
{
    private readonly ILogger<BlobMiddlewareHandler> _logger;
    private readonly BlobContainerClient _blobResubmitClient;

    /// <summary>
    /// the name of the container where blobs for resubmition are stored
    /// </summary>
    public const string BlobResubmitContainerName = "blob-resubmits";

    public BlobMiddlewareHandler(ILogger<BlobMiddlewareHandler> logger,IAzureClientFactory<BlobServiceClient> blobFact)
    {
        _logger = logger;
        _blobResubmitClient = blobFact
           .CreateClient(BlobResubmitContainerName)
           .GetBlobContainerClient(BlobResubmitContainerName);
    }

    public string BindingName => "blobTrigger";

    public async Task SaveIncomingRequest(FunctionContext context)
    {
        string invocationId = context.InvocationId;

        var triggerProperties = context.FunctionDefinition.Parameters
            .Where(atr => atr.Properties.ContainsKey("bindingAttribute"))
            .First().Properties;

        var sourceBlobNameProperty = Path.GetFileName(context.BindingContext.BindingData
            .First(d => d.Key.Equals("BlobTrigger")).Value!
            .ToString()!);

        if (triggerProperties.TryGetValue("bindingAttribute", out var bindingAttributeObj))
        {
            var bindingAttribute = bindingAttributeObj;

            if (bindingAttribute == null)
            {
                _logger.LogWarning("bindingAttribute is null");
                return;
            }

            // Extract connection/blob path from attribute
            var connectionProperty = bindingAttribute?.GetType().GetProperty("Connection")
                ?.GetValue(bindingAttribute)?.ToString();

            var blobPathProperty = bindingAttribute?.GetType().GetProperty("BlobPath")
                ?.GetValue(bindingAttribute)?.ToString();

            if (string.IsNullOrEmpty(blobPathProperty))
            {
                _logger.LogWarning("BlobPath not found in trigger attribute");
                return;
            }

            var sourceContainerProperty = Path.GetDirectoryName(blobPathProperty);
            if (string.IsNullOrEmpty(sourceContainerProperty))
            {
                _logger.LogWarning("Unable to extract container name from BlobPath");
                return;
            }

            try
            {
                // Create BlobClient of the incoming request
                BlobClient originalSourceBlobClient = await CreateBlobClientAsync(connectionProperty, sourceContainerProperty, sourceBlobNameProperty);

                if (originalSourceBlobClient != null)
                {
                    await originalSourceBlobClient.SaveBlobForResubmitionAsync(invocationId,destinationContainer:_blobResubmitClient);
                    _logger.LogInformation("Blob {BlobName} saved for resubmission with invocationId {InvocationId}",
                        sourceBlobNameProperty, invocationId);
                }
                else
                {
                    _logger.LogError("Failed to create BlobClient for blob {BlobName}", sourceBlobNameProperty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving blob {BlobName} for resubmission", sourceBlobNameProperty);
                throw;
            }
        }
    }

    /// <summary>
    /// Creates a BlobClient using the appropriate authentication method:
    /// 1. Connection string (if available)
    /// 2. Identity-based connection with service URI
    /// 3. Default to AzureWebJobsStorage connection string
    /// </summary>
    private async Task<BlobClient> CreateBlobClientAsync(string? connectionName, string containerName, string blobName)
    {
        try
        {

            if (string.IsNullOrEmpty(connectionName) || connectionName.Equals("AzureWebJobsStorage")) // default local storage connection
            {
                connectionName = "AzureWebJobsStorage";
                var connectionString = Environment.GetEnvironmentVariable(connectionName);

                if (!string.IsNullOrEmpty(connectionString))
                {
                    // Case 1: Connection string authentication
                    _logger.LogDebug("Using connection string authentication for connection '{ConnectionName}'", connectionName);
                    return new BlobClient(connectionString: connectionString, containerName, blobName);
                }
            }

            // Case 2: Identity-based connection (managed identity or user identity)
            // Look for service URIs configured as: {PREFIX}__serviceUri or {PREFIX}__blobServiceUri
            var serviceUri = GetServiceUri(connectionName);

            if (!string.IsNullOrEmpty(serviceUri))
            {
                _logger.LogDebug("Using identity-based authentication with service URI for connection '{ConnectionName}'", connectionName);
                
                var blobUri = new Uri($"{serviceUri.TrimEnd('/')}/{containerName}/{blobName}");
                
                // Use DefaultAzureCredential for managed identity (system-assigned or user-assigned)
                var credential = new DefaultAzureCredential();
                
                return new BlobClient(blobUri, credential);
            }

            // Case 3: Fallback - default to AzureWebJobsStorage connection string
            connectionName = "AzureWebJobsStorage";
            _logger.LogWarning("No configuration found for connection '{ConnectionName}', falling back to AzureWebJobsStorage", connectionName);
            var connString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            if (!string.IsNullOrEmpty(connString))
            {
                return new BlobClient(connectionString: connString, containerName, blobName);
            }

            _logger.LogError("Unable to create BlobClient: no connection string or identity-based URI found for connection '{ConnectionName}'", connectionName);
            return null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating BlobClient for connection '{ConnectionName}'", connectionName);
            throw;
        }
    }

    /// <summary>
    /// Gets the service URI from environment variables for identity-based connections.
    /// Checks for both combined serviceUri and separate blobServiceUri patterns.
    /// </summary>
    private string? GetServiceUri(string connectionName)
    {
        // Try service URI (works for blob-only scenarios)
        var serviceUri = Environment.GetEnvironmentVariable($"{connectionName}__serviceUri");
        if (!string.IsNullOrEmpty(serviceUri))
            return serviceUri;

        // Try blob-specific service URI (works for multi-service scenarios)
        serviceUri = Environment.GetEnvironmentVariable($"{connectionName}__blobServiceUri");
        if (!string.IsNullOrEmpty(serviceUri))
            return serviceUri;

        return null;
    }

    /// <summary>
    /// Extracts storage account name from connection string.
    /// Expected format: DefaultEndpointsProtocol=https;AccountName=xxx;AccountKey=yyy;EndpointSuffix=core.windows.net
    /// </summary>
    private string GetStorageAccountName(string connectionString)
    {
        var parts = connectionString.Split(';');
        var accountNamePart = parts.FirstOrDefault(p => p.StartsWith("AccountName="));
        return accountNamePart?.Substring("AccountName=".Length) ?? throw new InvalidOperationException("AccountName not found in connection string");
    }
}
