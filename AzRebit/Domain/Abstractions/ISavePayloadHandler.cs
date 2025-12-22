using AzRebit.Domain.Results;

using Microsoft.Azure.Functions.Worker;

namespace AzRebit.Domain.Abstractions;

public interface ISavePayloadHandler
{
    static string ResubmitFilePrefix { get; }
    public string BindingName { get; }
    public Task<RebitActionResult> SaveIncomingRequest(ISavePayloadCommand command);
}

public interface ISavePayloadCommand
{
    public FunctionContext Context { get;  }
}
