using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using AzRebit.Utilities;

using Microsoft.Azure.Functions.Worker;

namespace AzRebit.Shared.Model;

internal static class TriggerTypes
{
    internal enum TriggerType
    {
        Unknown, Blob, Queue, Http, Timer, EventHub, ServiceBus, Other
    }
    /// <summary>
    /// Lists all available trigger types and their corresponding Attribute types
    /// </summary>
    internal static readonly Dictionary<TriggerType,Type> AvailableTriggersAttributes = new() 
    {
        { TriggerType.Blob,typeof(BlobTriggerAttribute) },
        { TriggerType.Http,typeof(HttpTriggerAttribute) },
    };

}
