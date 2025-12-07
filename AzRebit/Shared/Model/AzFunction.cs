using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Shared.Model;

/// <summary>
/// Represents an Azure Function with its name and trigger details
/// </summary>
/// <param name="name"></param>
/// <param name="triggerType"></param>
/// <param name="triggerDetails">Trigger metadata like connection strings, const params etc.</param>
internal sealed class AzFunction(string name,TriggerType triggerType,Dictionary<string,string>? triggerDetails)
{
    internal TriggerType TriggerType => triggerType;
    internal string Name => name;
    /// <summary>
    /// Handler that is doing the resubmiting logic
    /// </summary>
    internal Dictionary<string,string> TriggerMetadata => triggerDetails;
}


