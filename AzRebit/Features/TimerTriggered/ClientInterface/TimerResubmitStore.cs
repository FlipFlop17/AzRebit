using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Features.TimerTriggered.ClientInterface;

/// <summary>
/// Stores all functions with ITimerResubmit interface
/// </summary>
internal class TimerResubmitStore
{

    public readonly Dictionary<string, ITimerResubmit> Handlers;

    public TimerResubmitStore(IEnumerable<ITimerResubmit> timerHandlers)
    {
            
    }


}
