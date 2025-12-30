using AzRebit.Features.RebitSave;
using AzRebit.Shared;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.FunctionExample.Features;

/// <summary>
/// Examples on how to use the resubmit feature inside Azure Functions
/// </summary>
public class TimerCats
{
    private readonly ILogger<TimerCats> _logger;
    private readonly IRebitManualSave _manualSave;
    private readonly List<string> _cats = new List<string> { "Tom", "Garfield", "Sylvester" };
    private bool deleteResubmitionFile = Environment.GetEnvironmentVariable("AZREBIT_DELETE_RESUBMITION_FILE") == "true";
    public TimerCats(ILogger<TimerCats> logger, IRebitManualSave manualSave)
    {
        _logger = logger;
        _manualSave = manualSave;
    }

    /// <summary>
    /// Timer trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("CheckCats")]
    public async Task<IActionResult> RunTimerCats(
        [TimerTrigger("* * 1 * * *"/* Every second, every minute, between 01:00 AM and 01:59 AM, every day */)] FunctionContext funcContext)
    {

        string someFileOrDataContentPickedUpByTheTimerFunction = "A cat's purr has healing properties";
        var manualSaveResult = await _manualSave.SavePayloadForResubmit(someFileOrDataContentPickedUpByTheTimerFunction, "CheckCats");


        //optional but recomended - if processing was successfull delete the file as you probably won't need it for resubmition to save storage space
        if (deleteResubmitionFile)
            await AzRebitUtils.DeleteSavedResubmitionBlobAsync(funcContext.InvocationId.ToString());

        return new OkObjectResult("Processing completed - triggered by a TimerTrigger");
    }
}