using AzRebit.Domain.Results;

using Microsoft.Azure.Functions.Worker;

namespace AzRebit.Domain.Abstractions;

internal interface ISavePayloadHandler
{
    string ResubmitFilePrefix { get; }
    public string BindingName { get; }
    public Task<RebitActionResult> SaveIncomingRequest(ISavePayloadCommand command);
}

internal interface ISavePayloadCommand
{
    public FunctionContext Context { get; }
}
