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
    internal static IEnumerable<AzFunction> DiscoverAzFunctions(IServiceCollection services,ISet<string>? excludedFunctionNames = null)
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
                            var func=CreateAzFunction(attr.Name, method.GetParameters(),services);
                            if(func is not null)
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
    private static AzFunction? CreateAzFunction(string funcName, ParameterInfo[] allParams,IServiceCollection services)
    {
        ICollection<TriggerSetupBase> supportedTriggers = DiscoverSupportedTriggerTypes().ToList(); //we only process triggers (IFeatureSetup) supported by this package
        //steps
        //find out whats the trigger atribute of the function
        //check if the trigger is in the supported range

        //create depending on the resolved trigger
        
        foreach (var triggerAttribute in supportedTriggers)
        {
            try
            {
                //var featureInstance = Activator.CreateInstance(feature) as IFeatureSetup;
                //if (featureInstance is null) continue;
                var triggerAttributeType = featureInstance.TriggerAttribute;
                //var triggerType = featureInstance.TriggerSupport;

                // resolve trigger attribute
                var functionsTriggerParameter = allParams.FirstOrDefault(p =>p.GetCustomAttribute(triggerAttributeType) != null);
                var functionTriggerType=allParams.Select(t=>t.ParameterType).FirstOrDefault();
                
                if (functionsTriggerParameter is not null)
                {
                    AzFunction func = triggerType switch
                    {
                        typeof(BlobTriggerAttribute) => Setup.CreateAzFunction(funcName, functionsTriggerParameter, services),
                        TriggerTypes.TriggerType.Queue => QueueFeatureSetup.CreateAzFunction(funcName, functionsTriggerParameter, services),
                        TriggerTypes.TriggerType.Http => HttpFeatureSetup.CreateAzFunction(funcName, functionsTriggerParameter, services)

                    };

                    return func;
                } else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not resolve the TriggerMetadata {triggerAttribute}: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Discovers all non-abstract classes in loaded assemblies that implement the IFeatureSetup interface.
    /// </summary>
    /// <remarks>Only assemblies that do not start with "System" or "Microsoft" are scanned. If an assembly
    /// cannot be loaded or inspected, it is skipped and a debug message is written. This method does not instantiate
    /// the handler types; it only returns their Type objects.</remarks>
    /// <returns>An enumerable collection of types representing trigger handler classes found in the current application domain.
    /// The collection may be empty if no handlers are found.</returns>
    private static IEnumerable<TriggerSetupBase> DiscoverSupportedTriggerTypes()
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

}
