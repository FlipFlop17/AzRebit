using AzRebit.Features.RebitClientStore;
using AzRebit.Shared;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.FunctionExample.Features;


/// <summary>
/// Examples on how to use the resubmit feature inside Azure Functions
/// </summary>
public class TimerCats:ITimerResubmit
{
    private readonly ILogger<TimerCats> _logger;
    private readonly IRebitStoreOperations _rebit;
    private readonly List<string> _cats = new List<string> { "Tom", "Garfield", "Sylvester" };
    private bool deleteResubmitionFile = Environment.GetEnvironmentVariable("AZREBIT_DELETE_RESUBMITION_FILE") == "true";
    public TimerCats(ILogger<TimerCats> logger, IRebitStoreOperations rebit)
    {
        _logger = logger;
        _rebit = rebit;
    }

    /// <summary>
    /// Timer trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("CheckCats")]
    public async Task<IActionResult> RunTimerCats(
        [TimerTrigger("0 * * * * *"/* Runs at second 0 of every minute */)] FunctionContext funcContext)
    {

        _logger.LogInformation("the timer trigger function has started");
        string someDataContentPickedUpByTheTimerFunction = "A cat's purr has healing properties";
        var manualSaveResult = await _rebit.SavePayloadForResubmit(
            payload: someDataContentPickedUpByTheTimerFunction,
            functionName: "CheckCats",
            id: funcContext.InvocationId);

        _logger.LogInformation($"save operation success:{manualSaveResult.IsSuccess}");


        await HandleTimerTrigger(someDataContentPickedUpByTheTimerFunction);
        
        
        //optional but recomended - if processing was successfull delete the file as you probably won't need it for resubmition to save storage space
        if (deleteResubmitionFile)
            await _rebit.DeleteResubmitFile(funcContext.InvocationId.ToString());

        return new OkObjectResult("Processing completed - triggered by a TimerTrigger");
    }

    /// <summary>
    /// Interface that will process/orchestrate the timer trigger businnes rules.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="payload"></param>
    /// <returns></returns>
    /// <remarks>When this method is called from the <code>/resubmit</code> endpoint it will receive the same data structure as saved with <code>IRebitStoreOperations.SavePayloadForResubmit</code>.
    /// Be sure to save the same data type/structure so HandleTimerTrigger knows how to process it.
    /// </remarks>
    public async Task HandleTimerTrigger<T>(T payload)
    {

    }
}