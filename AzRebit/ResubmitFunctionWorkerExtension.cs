using AzRebit.Domain.Entities;
using AzRebit.Features.RebitClientStore;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Infrastructure.StateStorage;
using AzRebit.Middleware;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AzRebit;

/// <summary> 
/// Extension for adding default resubmit functionality to Azure Functions
/// </summary>
public static class ResubmitFunctionWorkerExtension
{
    /// <summary>
    /// Represents the service client name used for resubmitting archived blobs to the storage account.
    /// </summary>
    public const string BlobResubmitServiceClientName = "StorageAccountResubmitArchive";
    /// <summary>
    /// Specifies the name of the internal storage table used for persisting Rebit state information.
    /// </summary>
    /// <remarks>
    ///  [FEATURE NOT ACTIVE]
    /// </remarks>
    public const string InternalRebitStorageTable = "RebitStatePersistTable";
    /// <summary>
    /// Provides options for configuring the resubmit pipeline, including specifying functions to exclude from
    /// resubmission.
    /// </summary>
    public class ResubmitOptions
    {
        /// <summary>
        /// Names of functions to be excluded from the resubmit pipeline.
        /// </summary>
        public HashSet<string> ExcludedFunctionNames { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Adds the AzRebit resubmit endpoint
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IFunctionsWorkerApplicationBuilder AddResubmitEndpoint(
        this IFunctionsWorkerApplicationBuilder builder,
        Action<ResubmitOptions>? configure = null)
    {
        var options = new ResubmitOptions();
        configure?.Invoke(options);
        bool stateStoragefeatureActive = false;
        // register options for dependency injection
        builder.Services.AddSingleton(Options.Create(options));
        // discover and register function names
        var discoveredFunctions = AssemblyDiscovery.DiscoverAndAddAzFunctions(builder.Services, options.ExcludedFunctionNames).ToList();
        builder.Services.AddSingleton<IReadOnlyCollection<AzFunction>>(discoveredFunctions);
        builder.Services.AddSingleton<IResubmitStorage, BlobResubmitStorage>();
        builder.Services.AddSingleton<IRebitStoreOperations, RebitStoreOperations>();
        if (stateStoragefeatureActive)
        {
            builder.Services.AddSingleton<IWorkItemStore, StorageTablePersistService>();
        }
        builder.Services.AddAzureClients(c =>
        {
            c.AddBlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")).WithName(BlobResubmitServiceClientName);
            c.AddTableServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")).WithName(InternalRebitStorageTable);
        });

        builder.UseMiddleware<SavePayloadsMiddleware>();
        builder.Services.AddAllAzRebitServices();

        return builder;
    }
}
