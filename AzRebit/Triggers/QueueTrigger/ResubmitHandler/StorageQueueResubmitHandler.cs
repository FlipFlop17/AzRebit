using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzRebit.Shared;
using AzRebit.Shared.Model;

namespace AzRebit.Triggers.QueueTrigger.ResubmitHandler;

internal class StorageQueueResubmitHandler : IResubmitHandler
{
    public TriggerTypes.TriggerType HandlerType => TriggerTypes.TriggerType.Queue;

    public Task<ActionResult> HandleResubmitAsync(string invocationId, object? triggerAttributeMetadata)
    {
        throw new NotImplementedException();
    }
}
