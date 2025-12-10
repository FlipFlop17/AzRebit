using System;
using System.Diagnostics;
using System.Reflection;

using AzRebit.Model;
using AzRebit.Shared;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AzRebit;

internal static class AssemblyDiscovery
{

    /// <summary>
    /// Discovers all Functions with the [Function(Name="...")] attribute, excluding internal resubmit functions
    /// </summary>
    /// <returns>IEnumerable of function names</returns>
    internal static IEnumerable<AzFunction> DiscoverAndAddAzFunctions(IServiceCollection services, ISet<string>? excludedFunctionNames = null)
    {
        var foundFunctions = new HashSet<AzFunction>();
        excludedFunctionNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ICollection<TriggerSetupBase> supportedTriggers = [.. DiscoverTriggerSetups()];

        var functionAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.GetReferencedAssemblies()
            .Any(ra => ra.Name == "Microsoft.Azure.Functions.Worker"));


        foreach (var functionProject in functionAssemblies)
        {
            try
            {
                var methodsDefinedWithFunctionAttribute = functionProject.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Select(m => new { Method = m, Attr = m.GetCustomAttribute<FunctionAttribute>() })
                .Where(x => x.Attr != null &&
                           !string.IsNullOrWhiteSpace(x.Attr.Name) &&
                           !excludedFunctionNames.Contains(x.Attr.Name));

                foreach (var function in methodsDefinedWithFunctionAttribute)
                {
                    //find out the atribute the function uses
                    var triggerType = ResolveTriggerAttribute(function.Method.GetParameters(), supportedTriggers);
                    var triggerBaseSetupClass = supportedTriggers.FirstOrDefault(t => t.TriggerAttribute == triggerType.GetType());
                    if (triggerBaseSetupClass != null)
                    {
                        foundFunctions.Add(triggerBaseSetupClass.TryCreateAzFunction(function.Attr.Name, triggerType, services));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error in DiscoverAndAddAzFunctions() {functionProject.FullName}: {ex.Message}");
            }
        }

        return foundFunctions;
    }
    /// <summary>
    /// Resolves what type of trigger is set on the functions signature
    /// </summary>
    /// <example>
    /// [TimerTrigger], [BlobTrigger]
    /// </example>
    /// <param name="allParams"></param>
    /// <param name="supportedTriggers"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    private static TriggerBindingAttribute ResolveTriggerAttribute(ParameterInfo[] allParams, ICollection<TriggerSetupBase> supportedTriggers)
    {
        var triggerTypes=supportedTriggers.Select(t=>t.TriggerAttribute).ToList();

        var triggerParam = allParams
            .Select(p => new
            {
                Parameter = p,
                Attribute = triggerTypes
                    .Select(t => p.GetCustomAttribute(t))
                    .FirstOrDefault(a => a != null)
            })
            .FirstOrDefault(x => x.Attribute != null);

        if (triggerParam?.Attribute is null)
            throw new ArgumentNullException(nameof(triggerParam), "No trigger attribute found");

        return (TriggerBindingAttribute)triggerParam.Attribute;

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
            .FirstOrDefault(a => a.ManifestModule.Name == "AzRebit.dll");

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
                    Console.WriteLine($"Could not instantiate {type.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not load supported trigger feature from");
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

        var installers = thisAssembly.GetTypes()
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

    /// <summary>
    /// Returns the full connection string app settings name from app settings
    /// </summary>
    /// <param name="connectionAttribute"></param>
    /// <returns></returns>
    public static string ResolveConnectionStringAppSettingName(string? connectionAttribute)
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
