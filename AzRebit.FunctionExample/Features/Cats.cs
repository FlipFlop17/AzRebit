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
public class Cats
{
    private readonly ILogger<Cats> _logger;

    public Cats(ILogger<Cats> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Http request trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("GetCats")]
    public async Task<HttpResponseData> RunGet([HttpTrigger(AuthorizationLevel.Anonymous, "get")] 
    HttpRequestData req,FunctionContext funcContext)
    {
        var response=req.CreateResponse();
        _logger.LogInformation("incoming payload saved");
        response.StatusCode = System.Net.HttpStatusCode.OK;

        // ... some important work

        //optional - if processing was successfull
        //await AzRebitBlobExtensions.DeleteSavedResubmitionBlobAsync(funcContext.InvocationId.ToString());

        await response.WriteStringAsync("I was triggered by a Http request - This request is automatically saved in this function storage account and ready for resubmition");
        
        
        return response;
    }
    /// <summary>
    /// Timer trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("CheckCats")]
    public async Task<IActionResult> RunTimerCats(
        [TimerTrigger("")] FunctionContext funcContext)
    {
        _logger.LogInformation("incoming payload saved");

        //optionall - if processing was successfull
        //await AzRebitBlobExtensions.DeleteSavedResubmitionBlobAsync(funcContext.InvocationId.ToString());

        return new OkObjectResult("I was triggered by a TimerTrigger! - This request is automatically saved in this function storage account and ready for resubmition");
    }


    /// <summary>
    /// Blob trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("TransferCats")]
    public async Task<IActionResult> RunAdd(
        [BlobTrigger("cats-container/{blobPath}",Connection ="AzureWebJobsStorage")] BlobClient blobClient,string blobPath,
        FunctionContext funcContext)
    {
        _logger.LogInformation("incoming payload saved");
        
        //optionall - if processing was successfull
        //await AzRebitBlobExtensions.DeleteSavedResubmitionBlobAsync(funcContext.InvocationId.ToString());

        return new OkObjectResult("I was triggered by a BlobTrigger! - This request is automatically saved in this function storage account and ready for resubmition");
    }


    //[Function("GetDogs")]
    //public async Task<IActionResult> RunAdd(
    //    [QueueTrigger("my-container/{blobPath}", Connection = "AzureWebJobsStorage")] BlobClient blobClient, string blobPath,
    //    FunctionContext funcContext)
    //{
    //    _logger.LogInformation("incoming payload saved");
    //    return new OkObjectResult("I was triggered by a BlobTrigger! - This request is automatically saved in this function storage account and ready for resubmition");
    //}
}