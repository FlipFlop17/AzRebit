using AzRebit.Model;

using Microsoft.Azure.Functions.Worker;

namespace AzRebit.Shared;

public interface ISavePayloadsHandler
{
    public const string BlobTagInvocationId = "InvocationId";
    public string BindingName { get; }
    public Task<RebitActionResult> SaveIncomingRequest(FunctionContext context);
}
