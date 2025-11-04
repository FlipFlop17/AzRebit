using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Shared;

internal interface ITriggerHandler
{
    TriggerType HandlerType { get; }
    internal Task HandleResubmitAsync<T>(T triggerDetails, string invocationId);
}
