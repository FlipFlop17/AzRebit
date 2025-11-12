namespace AzRebit.Shared.Model;

internal static class TriggerTypes
{
    internal enum TriggerType
    {
        Unknown, Blob, Queue, Http, Timer, EventHub, ServiceBus
    }

}
