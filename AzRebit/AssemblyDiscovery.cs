using System.Diagnostics;
using System.Reflection;

using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.BlobTriggered;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace AzRebit;

internal static class AssemblyDiscovery
{

    /// <summary>
    /// Discovers all Functions with the [Function(Name="...")] attribute, excluding internal resubmit functions
    /// </summary>
    /// <returns>IEnumerable of function names</returns>
    internal static IEnumerable<AzFunction> DiscoverAzFunctions()
    {
        var functionDetails = new HashSet<AzFunction>();
        var internalFunctions = new[] { "ResubmitHandler", "CleanupSavedResubmits" }; //functions that we want to skip in out search

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
                        var attr = method.GetCustomAttribute<FunctionAttribute>(); //we are looking for [Function("FunctionName")] which is of type FunctionAttribute
                        if (attr != null &&
                            !string.IsNullOrWhiteSpace(attr.Name) &&
                            !internalFunctions.Contains(attr.Name, StringComparer.OrdinalIgnoreCase))
                        {
                            AddAzFuncTriggerMetadata(attr.Name, functionDetails, method.GetParameters());
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

        return functionDetails;
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
    private static void AddAzFuncTriggerMetadata(string funcName, HashSet<AzFunction> functions, ParameterInfo[] allParams)
    {
        ICollection<Type> supportedTriggers = DiscoverSupportedTriggerTypes().ToList(); //we only search for triggers supported by this project
        foreach (var feature in supportedTriggers)
        {
            try
            {
                var featureInstance = Activator.CreateInstance(feature) as IFeatureSetup;
                var triggerAttributeType = featureInstance.TriggerAttribute;
                var triggerType = featureInstance.TriggerSupport;

                // Check if any parameter has this trigger attribute
                var functionsTriggerParameter = allParams.FirstOrDefault(p =>p.GetCustomAttribute(triggerAttributeType) != null);

                if (functionsTriggerParameter is not null)
                {
                    //each trigger feature knows how to create its own metadata from the parameter info
                    var triggerDetails = featureInstance.CreateTriggerMetadata(functionsTriggerParameter);
                    functions.Add(new AzFunction(funcName, triggerType, triggerDetails!));
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not resolve the TriggerMetadata {feature}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Discovers all non-abstract classes in loaded assemblies that implement the ITriggerHandler interface.
    /// </summary>
    /// <remarks>Only assemblies that do not start with "System" or "Microsoft" are scanned. If an assembly
    /// cannot be loaded or inspected, it is skipped and a debug message is written. This method does not instantiate
    /// the handler types; it only returns their Type objects.</remarks>
    /// <returns>An enumerable collection of types representing trigger handler classes found in the current application domain.
    /// The collection may be empty if no handlers are found.</returns>
    private static IEnumerable<Type> DiscoverSupportedTriggerTypes()
    {
        var triggerHandlerType = typeof(IFeatureSetup);
        var featuresSetupClasses = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip system assemblies
            if (assembly.FullName?.StartsWith("System") == true ||
                assembly.FullName?.StartsWith("Microsoft") == true)
                continue;

            try
            {
                var featureSetupClasses = assembly.GetTypes()
                    .Where(t => t is { IsClass: true, IsAbstract: false }
                        && triggerHandlerType.IsAssignableFrom(t));

                featuresSetupClasses.AddRange(featureSetupClasses);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not load supported trigger feature from assembly {assembly.FullName}: {ex.Message}");
            }
        }

        return featuresSetupClasses;
    }

    /// <summary>
    /// Automatically discovers and registers all feature modules implementing IFeatureModule.
    /// </summary>
    /// <remarks>When adding a new trigger feature this interface is used to recognize it and to be automaticalyy added in DI</remarks>
    internal static void RegisterAllFeatures(IServiceCollection services)
    {
        var featureSetupClass = typeof(IFeatureSetup);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip system assemblies
            if (assembly.FullName?.StartsWith("System") == true ||
                assembly.FullName?.StartsWith("Microsoft") == true)
                continue;

            try
            {
                var moduleTypes = assembly.GetTypes()
                    .Where(t => t is { IsClass: true, IsAbstract: false }
                        && featureSetupClass.IsAssignableFrom(t));

                foreach (var moduleType in moduleTypes)
                {
                    var method = moduleType.GetMethod(nameof(IFeatureSetup.RegisterServices),
                        BindingFlags.Public | BindingFlags.Static);

                    if (method != null)
                    {
                        method.Invoke(null, new object[] { services });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not load feature setup class from assembly {assembly.FullName}: {ex.Message}");
            }
        }
    }

}
