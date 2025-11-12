using AzRebit;
using AzRebit.Extensions;

using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzRebit.FunctionExample.Features;

/// <summary>
/// Examples on how to use the resubmit feature inside Azure Functions
/// </summary>
public class GetCats
{
    private readonly ILogger<GetCats> _logger;

    public GetCats(ILogger<GetCats> logger)
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

        //optionall - if processing was successfull
        await AzRebitBlobExtensions.DeleteSavedBlobAsync(funcContext.InvocationId.ToString());

        await response.WriteStringAsync("I was triggered by a Http request - This request is automatically saved in this function storage account and ready for resubmition");
        
        
        return response;
    }

    /// <summary>
    /// Blob trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("AddCat")]
    public async Task<IActionResult> RunAdd(
        [BlobTrigger("my-container/{blobPath}",Connection ="AzureWebJobsStorage")] BlobClient blobClient,string blobPath,
        FunctionContext funcContext)
    {
        _logger.LogInformation("incoming payload saved");
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