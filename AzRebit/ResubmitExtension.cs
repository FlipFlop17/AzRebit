using System.Reflection;

using AzRebit.Discovery;
using AzRebit.Endpoints.CleanUp;
using AzRebit.Endpoints.Resubmit;
using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.BlobTriggered.Handler;
using AzRebit.Triggers.BlobTriggered.Middleware;
using AzRebit.Triggers.HttpTriggered.Handler;
using AzRebit.Triggers.HttpTriggered.Middleware;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AzRebit;

/// <summary> 
/// Extension for adding default resubmit functionality to Azure Functions
/// </summary>
public static class ResubmitExtension
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
        var functionDetails = CollectFunctionDetails().ToList();
        builder.Services.AddSingleton<IReadOnlyCollection<AzFunction>>(functionDetails);
        
        // adding handlers for different trigger types
        builder.Services.AddSingleton<ITriggerHandler, BlobTriggerHandler>();
        builder.Services.AddSingleton<ITriggerHandler, HttpTriggerHandler>();

        //adding all middleware handlers 
        builder.Services.AddSingleton<BlobResubmitMiddleware>();
        builder.Services.AddSingleton<HttpResubmitMiddleware>();

        // register the resubmit endpoint function
        builder.Services.AddSingleton<ResubmitEndpoint>();
        if (options.AddCleanUpFunction)
        {
            builder.Services.AddSingleton<CleanUpFunction>();
        }

        return builder;
    }
    /// <summary>
    /// Configures the Functions Worker to use blob resubmit middleware
    /// </summary>
    public static IFunctionsWorkerApplicationBuilder UseResubmitMiddleware(
        this IFunctionsWorkerApplicationBuilder builder)
    {
        builder.UseMiddleware<BlobResubmitMiddleware>();
        builder.UseMiddleware<HttpResubmitMiddleware>();
        return builder;
    }

    /// <summary>
    /// Discovers all Functions with the [Function(Name="...")] attribute, excluding internal resubmit functions
    /// </summary>
    /// <returns>IEnumerable of function names</returns>
    internal static IEnumerable<AzFunction> CollectFunctionDetails()
    {
        var functionDetails = new HashSet<AzFunction>();
        var internalFunctions = new[] { "ResubmitHandler" };

        // Loop through all loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip system assemblies to avoid noise
            if (assembly.FullName?.StartsWith("System") == true ||
                assembly.FullName?.StartsWith("Microsoft") == true)
                continue;

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                    {
                        var attr = method.GetCustomAttribute<FunctionAttribute>();
                        if (attr != null && 
                            !string.IsNullOrWhiteSpace(attr.Name) &&
                            !internalFunctions.Contains(attr.Name, StringComparer.OrdinalIgnoreCase))
                        {
                            AddTriggerDetails(attr.Name,functionDetails, method.GetParameters());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Skip assemblies that can't be loaded
                System.Diagnostics.Debug.WriteLine($"Could not load types from assembly {assembly.FullName}: {ex.Message}");
            }
        }

        return functionDetails;
    }

    private static void AddTriggerDetails (string funcName,HashSet<AzFunction> functions, ParameterInfo[] allParams)
    {
        foreach (var trigger in TriggerTypes.AvailableTriggersAttributes) {
            //check if we have a trigger of this type
            Type searchAttribute=trigger.Value;
            var foundTriggerAttribute = allParams.FirstOrDefault(p => p.GetCustomAttribute(searchAttribute) != null);
            
            if (foundTriggerAttribute is not null ) {
                //create trigger details for found triggerAttribute
                object? triggerDetails = TriggerDetailsFactory.CreateTriggerDetails(trigger.Key,foundTriggerAttribute);
                if (triggerDetails is not null)
                    functions.Add(new AzFunction(funcName, trigger.Key, triggerDetails));
                return;
            }
        }
    }

}
