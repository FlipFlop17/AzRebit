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
public class HttpCats
{
    private readonly ILogger<HttpCats> _logger;
    private readonly List<string> _cats=new List<string> { "Tom", "Garfield", "Sylvester" };
    private bool deleteResubmitionFile = Environment.GetEnvironmentVariable("AZREBIT_DELETE_RESUBMITION_FILE") == "true";
    public HttpCats(ILogger<HttpCats> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Http request trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("GetCats")]
    public async Task<HttpResponseData> RunGet([HttpTrigger(AuthorizationLevel.Anonymous, "get","post")] 
    HttpRequestData req,FunctionContext funcContext)
    {
        var response=req.CreateResponse();
        response.StatusCode = System.Net.HttpStatusCode.OK;

        // ... some important work

        //cleanup
        //optional but recomended - if processing was successfull delete the file as we probably won't need it for resubmition to save storage space
        if (deleteResubmitionFile)
            await AzRebitBlobExtensions.DeleteSavedResubmitionBlobAsync(funcContext.InvocationId.ToString());

        await response.WriteAsJsonAsync(_cats);

        return response;
    }

}