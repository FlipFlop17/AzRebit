using AzRebit.Model;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;

namespace AzRebit.Shared;

public interface ISavePayloadHandler
{
    public string BindingName { get; }
    public Task<RebitActionResult> SaveIncomingRequest(ISavePayloadCommand command);
}

public interface ISavePayloadCommand
{
    public FunctionContext Context { get;  }
}
