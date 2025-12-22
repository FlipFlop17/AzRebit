using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Middleware;

//todo : make this a record
internal sealed class SaveIncomingRequestMiddlewareResponse
{
    public string InvocationId { get; set;  }
}
