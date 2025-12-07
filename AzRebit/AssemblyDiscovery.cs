using System.Diagnostics;
using System.Reflection;

using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.BlobTriggered;
using AzRebit.Triggers.HttpTriggered;
using AzRebit.Triggers.QueueTrigger;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AzRebit;

internal static class AssemblyDiscovery
{

    /// <summary>
    /// Discovers all Functions with the [Function(Name="...")] attribute, excluding internal resubmit functions
    /// </summary>
    /// <returns>IEnumerable of function names</returns>
    internal static IEnumerable<AzFunction> DiscoverAzFunctions(IServiceCollection services, ISet<string>? excludedFunctionNames = null)
    {
        var foundFunctions = new HashSet<AzFunction>();
        excludedFunctionNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Loop through all loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip system assemblies to avoid noise
            if (assembly.FullName?.StartsWith("System") == true ||
                assembly.FullName?.StartsWith("Microsoft") == true ||
                assembly.FullName?.Equals("AzRebit") == true)
                continue;

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                    {
                        var attr = method.GetCustomAttribute<FunctionAttribute>(); //we are looking for [Function("FunctionName")] which is of type FunctionAttribute
                        if (attr != null &&
                            !string.IsNullOrWhiteSpace(attr.Name) &&
                            !excludedFunctionNames.Contains(attr.Name))
                        {
                            var func = CreateAzFunction(attr.Name, method.GetParameters(), services);
                            if (func is not null)
                                foundFunctions.Add(func);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Skip assemblies that can't be loaded
                Debug.WriteLine($"Could not discover functions from assembly {assembly.FullName}: {ex.Message}");
            }
        }

        return foundFunctions;
    }

    /// <summary>
    /// Identifies the trigger type for the specified function and adds trigger details to the provided collection if a
    /// matching trigger attribute is found.
    /// </summary>
    /// <remarks>This method inspects the parameters of the specified function to determine if any are
    /// decorated with a recognized trigger attribute. If a matching trigger is found, the function and its trigger type
    /// are added to the provided collection. If no trigger is found, the collection remains unchanged.</remarks>
    /// <param name="funcName">The name of the function for which trigger details are being added.</param>
    /// <param name="functions">A collection to which the function and its trigger type will be added if a matching trigger is found.</param>
    /// <param name="allParams">An array of all parameters for the function, used to search for trigger attributes.</param>
    private static AzFunction? CreateAzFunction(string funcName, ParameterInfo[] allParams, IServiceCollection services)
    {
        ICollection<TriggerSetupBase> supportedTriggers = [.. DiscoverTriggerSetups()]; 

        foreach (TriggerSetupBase triggerBase in supportedTriggers)
        {
            try
            {
                AzFunction func = triggerBase.TryCreateAzFunction(funcName, allParams, services);
                return func;
            }
            catch (AzFunctionNotCreatedException ex)
            {
                Console.WriteLine("function not created {Reason}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not resolve the TriggerMetadata");
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Discovers all non-abstract classes in loaded assemblies that implement the IFeatureSetup interface.
    /// </summary>
    /// <remarks>Only assemblies that do not start with "System" or "Microsoft" are scanned. If an assembly
    /// cannot be loaded or inspected, it is skipped and a debug message is written. This method does not instantiate
    /// the handler types; it only returns their Type objects.</remarks>
    /// <returns>An enumerable collection of types representing trigger handler classes found in the current application domain.
    /// The collection may be empty if no handlers are found.</returns>
    private static IEnumerable<TriggerSetupBase> DiscoverTriggerSetups()
    {
        var featureInstances = new List<TriggerSetupBase>();

        try
        {
            var thisAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "AzRebit");

            var setupTypes = thisAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                && typeof(TriggerSetupBase).IsAssignableFrom(t));

            foreach (var type in setupTypes)
            {
                try
                {
                    var instance = (TriggerSetupBase)Activator.CreateInstance(type)!;
                    featureInstances.Add(instance);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not instantiate {type.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not load supported trigger feature from");
        }
        return featureInstances;

    }

    /// <summary>
    /// Automatically discovers and registers all Triggers features implementing ITriggersServiceCollection.
    /// </summary>
    /// <remarks>When adding a new trigger feature this interface is used to recognize it and to be automaticalyy added in DI</remarks>
    internal static IServiceCollection AddAllAzRebitServices(this IServiceCollection services)
    {

        var thisAssembly = AppDomain.CurrentDomain.GetAssemblies()
           .FirstOrDefault(a => a.GetName().Name == "AzRebit");

        var installers = thisAssembly.GetExportedTypes()
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsInterface &&
                    typeof(ITriggersServiceCollection).IsAssignableFrom(t))
                .Select(Activator.CreateInstance)
                .Cast<ITriggersServiceCollection>()
                .ToList();

        installers.ForEach(installer => installer.RegisterServices(services));

        return services;
    }

    public static string ResolveConnectionString(string? connectionAttribute)
    {
        // Default to AzureWebJobsStorage if not specified
        if (string.IsNullOrWhiteSpace(connectionAttribute))
        {
            return ValidateConnectionExists("AzureWebJobsStorage");
        }

        // Try the connection name as-is first
        if (ConnectionStringExists(connectionAttribute))
        {
            return connectionAttribute;
        }

        // Try with AzureWebJobsStorage: prefix (colon separator)
        var prefixedWithColon = $"AzureWebJobsStorage:{connectionAttribute}";
        if (ConnectionStringExists(prefixedWithColon))
        {
            return prefixedWithColon;
        }

        // Try with AzureWebJobsStorage__ prefix (double underscore for env vars)
        var prefixedWithUnderscore = $"AzureWebJobsStorage__{connectionAttribute}";
        if (ConnectionStringExists(prefixedWithUnderscore))
        {
            return prefixedWithUnderscore;
        }

        // If nothing found, return the original and let it fail downstream with proper error
        return connectionAttribute;
    }

    private static bool ConnectionStringExists(string connectionName)
    {
        // Check environment variables
        var envValue = Environment.GetEnvironmentVariable(connectionName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return true;
        }

        // If you have access to IConfiguration, check there too
        // var configValue = _configuration[connectionName];
        // if (!string.IsNullOrWhiteSpace(configValue))
        // {
        //     return true;
        // }

        return false;
    }

    private static string ValidateConnectionExists(string connectionName)
    {
        if (!ConnectionStringExists(connectionName))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionName}' not found in environment variables or configuration.");
        }
        return connectionName;
    }

}
