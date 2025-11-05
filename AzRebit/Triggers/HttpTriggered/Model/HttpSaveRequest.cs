using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.HttpTriggered.Model;

internal record HttpSaveRequest(string Id, 
    string Method,
    string Url,
    string QueryString, 
    IDictionary<string, string?>? Headers, 
    string Body, 
    DateTime TimestampUtc
    );

internal class HttpTriggerDetails
{
    public TriggerType TypeOfTriger { get; } = TriggerType.Http;
    public string Route { get; set; } = string.Empty;
}