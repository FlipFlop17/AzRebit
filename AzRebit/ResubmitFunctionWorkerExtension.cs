using AzRebit.Middleware;
using AzRebit.Shared.Model;

using Azure.Data.Tables;

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

        // register options for dependency injection
        builder.Services.AddSingleton(Options.Create(options));
        // discover and register function names
        var functionDetails = AssemblyDiscovery.DiscoverAzFunctions(options.ExcludedFunctionNames).ToList();
        builder.Services.AddSingleton<IReadOnlyCollection<AzFunction>>(functionDetails);
        builder.Services.AddAzureClients(c=>
        {
            c.AddTableServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"))
            .WithName("rebitTables");
        });

        builder.UseMiddleware<ResubmitMiddleware>();
        builder.Services.AddAllAzRebitServices();

        return builder;
    }
}
