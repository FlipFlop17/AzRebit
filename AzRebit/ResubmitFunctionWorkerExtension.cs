using AzRebit.Middleware;
using AzRebit.Shared.Model;

using Microsoft.Azure.Functions.Worker;
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

    public static IFunctionsWorkerApplicationBuilder AddResubmitEndpoint(
        this IFunctionsWorkerApplicationBuilder builder,
        Action<ResubmitOptions>? configure = null)
    {
        var options = new ResubmitOptions();
        configure?.Invoke(options);

        // register options for dependency injection
        builder.Services.AddSingleton(Options.Create(options));

        // discover and register function names
        var functionDetails = AssemblyDiscovery.DiscoverAzFunctions().ToList();
        builder.Services.AddSingleton<IReadOnlyCollection<AzFunction>>(functionDetails);
        //builder.Services.AddSingleton<ResubmitMiddleware>();
        builder.UseMiddleware<ResubmitMiddleware>();
        AssemblyDiscovery.RegisterAllFeatures(builder.Services);

        return builder;
    }
}
