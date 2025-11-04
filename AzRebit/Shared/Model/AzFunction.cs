using static AzRebit.Shared.Model.TriggerTypes;
using static AzRebit.Utilities.Utility;

namespace AzRebit.Shared.Model;

/// <summary>
/// Represents an Azure Function with its name and trigger details
/// </summary>
/// <param name="name"></param>
/// <param name="triggerType"></param>
/// <param name="triggerDetails"></param>
internal sealed class AzFunction(string name,TriggerType triggerType,object triggerDetails)
{
    internal TriggerType TriggerType => triggerType;
    internal string Name => name;   
    internal object TriggerDetails => triggerDetails;
}


