using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.BlobTriggered.Model;

internal class BlobTriggerDetails
{
    internal TriggerType TypeOfTriger { get; set; }
    internal string? ContainerName { get; set; }
    internal string? TriggerPath { get; set; }
    internal string? ConnectionString { get; set; }
}
