using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzRebit.Model;

using Microsoft.Azure.Functions.Worker;

namespace AzRebit.Shared;

public interface ISavePayloadsHandler
{
    public const string BlobTagInvocationId = "InvocationId";
    public const string BlobTagResubmitCount = "ResubmitCount";
    public string BindingName { get; }
    public Task<RebitActionResult> SaveIncomingRequest(FunctionContext context);
}
