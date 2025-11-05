namespace AzRebit.Triggers.HttpTriggered.Model;

internal record HttpSaveRequest(string Id, 
    string Method,
    string Url,
    string QueryString, 
    IDictionary<string, string?>? Headers, 
    string Body, 
    DateTime TimestampUtc
    );