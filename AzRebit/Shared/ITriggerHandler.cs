using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Shared;

internal interface ITriggerHandler
{
    /// <summary>
    /// Name of the blob container used to store resubmit requests for this trigger type.
    /// </summary>
    string ContainerName { get; }
    TriggerType HandlerType { get; }
    internal Task HandleResubmitAsync<T>(T triggerDetails, string invocationId);
}
