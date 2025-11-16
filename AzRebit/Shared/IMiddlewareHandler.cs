using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

namespace AzRebit.Shared;

internal interface IMiddlewareHandler
{
    public const string BlobTagInvocationId = "InvocationId";
    public string BindingName { get; }
    public Task SaveIncomingRequest(FunctionContext context);
}
