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
        /// Automatic cleanup of saved requests older than specified days.
        /// </summary>
        public int DaysToKeepRequests { get; set; } = 4;
        /// <summary>
        /// If true adds a new cleanup function that runs once a day at 01 (not configurable) to delete all saved requests older than 'DaysToKeepRequests'
        /// </summary>
        public bool AddCleanUpFunction { get; set; } = false;

    }
    public static IFunctionsWorkerApplicationBuilder AddResubmitEndpoint(this IFunctionsWorkerApplicationBuilder builder)
       => builder.AddResubmitEndpoints(_ => { });

    public static IFunctionsWorkerApplicationBuilder AddResubmitEndpoints(
        this IFunctionsWorkerApplicationBuilder builder,
        Action<ResubmitOptions> configure)
    {
        var options = new ResubmitOptions();
        configure(options);

        // register options for dependency injection
        builder.Services.AddSingleton(Options.Create(options));

        // discover and register function names
        var functionDetails = AssemblyDiscovery.DiscoverAzFunctions().ToList();
        builder.Services.AddSingleton<IReadOnlyCollection<AzFunction>>(functionDetails);
        //builder.Services.AddSingleton<ResubmitMiddleware>();
        builder.UseMiddleware<ResubmitMiddleware>();
        AssemblyDiscovery.RegisterAllFeatures(builder.Services);

        // register the resubmit endpoint function
        //builder.Services.AddSingleton<ResubmitEndpoint>();
        //if (options.AddCleanUpFunction)
        //{
        //    builder.Services.AddSingleton<CleanUpFunction>();
        //}
        
        return builder;
    }

    

}
