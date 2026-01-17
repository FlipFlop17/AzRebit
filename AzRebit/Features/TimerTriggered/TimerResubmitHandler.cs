using System.Reflection;

using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Domain.Results;
using AzRebit.Features.TimerTriggered.ClientInterface;
using AzRebit.Infrastructure.FileStorage;

using Microsoft.Azure.Functions.Worker;

namespace AzRebit.Features.TimerTriggered;

internal class TimerResubmitHandler(IResubmitStorage storage, IEnumerable<ITimerResubmit> timerHandlers) : IResubmitHandler
{
    public TriggerType HandlerType => TriggerType.Timer;

    public async Task<RebitResult<ResubmitHandlerResponse>> HandleResubmitAsync(string invocationId, AzFunction function)
    {
        //todo i need to call the functions ITimertrigger implementation and pass in the payload that was saved by that function
        var blobclient =await storage.FindAsync(invocationId);
        if (blobclient is null) return RebitResult<ResubmitHandlerResponse>.Failure(invocationId,Domain.Exceptions.AzRebitErrorType.BlobResubmitFileNotFound);

        //2. find this AzFunction that implements the ITimerResubmit and call the Handdler method and pass in the payload
        var timerHandler = timerHandlers.FirstOrDefault(h =>
        {
            var functionAttr = h.GetType().GetCustomAttribute<FunctionAttribute>();
            return functionAttr?.Name == function.Name;
        });
        await timerHandler.HandleTimerTriggerAsync();

        throw new NotImplementedException();
    }
}
