using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzRebit.Model;

using Microsoft.Azure.Functions.Worker;

namespace AzRebit.Shared;

public interface IMiddlewareHandler
{
    public const string BlobTagInvocationId = "InvocationId";
    public const string BlobTagResubmitCount = "ResubmitCount";
    public const string BlobPrefixForBlob = "bl-";
    public const string BlobPrefixForQueue = "qu-";
    public const string BlobPrefixForHttp = "ht-";
    public string BindingName { get; }
    public Task<RebitActionResult> SaveIncomingRequest(FunctionContext context);
}
