namespace AzRebit.HttpTriggered.Model;

internal record HttpSaveRequest(string Id, 
    string Method,
    string Url,
    string QueryString, 
    Dictionary<string, string> Headers, 
    string Body, 
    DateTime TimestampUtc
    );