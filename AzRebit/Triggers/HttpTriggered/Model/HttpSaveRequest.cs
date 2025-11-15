namespace AzRebit.Triggers.HttpTriggered.Model;


/// <summary>
/// Model of HTTP request to be saved
/// </summary>
/// <param name="Id"></param>
/// <param name="Method"></param>
/// <param name="Url"></param>
/// <param name="QueryString"></param>
/// <param name="Headers"></param>
/// <param name="Body"></param>
/// <param name="TimestampUtc"></param>
public record HttpSaveRequest(string Id, 
    string Method,
    string Url,
    string? QueryString, 
    IDictionary<string, string?>? Headers, 
    string? Body, 
    DateTime TimestampUtc
    );