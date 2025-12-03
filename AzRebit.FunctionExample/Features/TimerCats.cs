using System.Text.Json;

using AzRebit.HelperExtensions;
using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzRebit.FunctionExample.Features;

/// <summary>
/// Examples on how to use the resubmit feature inside Azure Functions
/// </summary>
public class TimerCats
{
    private readonly ILogger<TimerCats> _logger;
    private readonly List<string> _cats=new List<string> { "Tom", "Garfield", "Sylvester" };
    private bool deleteResubmitionFile = Environment.GetEnvironmentVariable("AZREBIT_DELETE_RESUBMITION_FILE") == "true";
    public TimerCats(ILogger<TimerCats> logger)
    {
        _logger = logger;
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

        //optional but recomended - if processing was successfull delete the file as we probably won't need it for resubmition to save storage space
        if (deleteResubmitionFile)
            await AzRebitBlobExtensions.DeleteSavedResubmitionBlobAsync(funcContext.InvocationId.ToString());

        return new OkObjectResult("I was triggered by a TimerTrigger! - This request is automatically saved in this function storage account and ready for resubmition");
    }
}