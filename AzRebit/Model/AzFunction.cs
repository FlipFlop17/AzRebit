using static AzRebit.Model.TriggerTypes;

namespace AzRebit.Model;

/// <summary>
/// Represents an Azure Function with its name and trigger details
/// </summary>
/// <param name="name"></param>
/// <param name="triggerType"></param>
/// <param name="triggerDetails">Trigger metadata like connection strings, const params etc.</param>
internal sealed class AzFunction(string name,TriggerName triggerType,Dictionary<string,string>? triggerDetails)
{
    internal TriggerName TriggerType => triggerType;
    internal string Name => name;
    /// <summary>
    /// Handler that is doing the resubmiting logic
    /// </summary>
    internal Dictionary<string,string> TriggerMetadata => triggerDetails;


    public void AddBlobContainerName(string blobContainerName)
    { 
        TriggerMetadata.Add("container",blobContainerName);
    }

}


