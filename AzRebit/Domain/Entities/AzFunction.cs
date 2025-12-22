using AzRebit.Domain.Enums;

namespace AzRebit.Domain.Entities;

/// <summary>
/// Represents an Azure Function with its name and trigger details
/// </summary>
/// <param name="name"></param>
/// <param name="triggerType"></param>
/// <param name="triggerDetails">Trigger metadata like connection strings, const params etc.</param>
internal sealed class AzFunction(string name,TriggerType triggerType)
{
    internal TriggerType TriggerType => triggerType;
    internal string Name => name;
    /// <summary>
    /// Handler that is doing the resubmiting logic
    /// </summary>
    internal Dictionary<string,string> TriggerMetadata { get; } = new();

    /// <summary>
    /// Adds the name of the input container for blob triggered functions
    /// </summary>
    /// <param name="containerName"></param>
    internal void AddFunctionTriggerContainerName(string containerName)
    { 
        TriggerMetadata.Add("trigger-container",containerName);
    }

    /// <summary>
    /// Gets the name of the trigger container of the function
    /// </summary>
    /// <returns></returns>
    internal string? GetFunctionTriggerContainerName() 
    {
        return TriggerMetadata.GetValueOrDefault("trigger-container");
    }

    /// <summary>
    /// Adds the name of the input queue for queue triggered functions
    /// </summary>
    /// <param name="containerName"></param>
    internal void AddFunctionTriggerQueueName(string queueName)
    {
        TriggerMetadata.Add("trigger-queue", queueName);
    }

    /// <summary>
    /// Gets the name of the trigger queue of the function
    /// </summary>
    /// <returns></returns>
    internal string? GetFunctionTriggerQueueName()
    {
        return TriggerMetadata.GetValueOrDefault("trigger-queue");
    }

}


