using AzRebit.Features.RebitClientStore;
using AzRebit.Shared;
using AzRebit.Shared.Extensions;

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
    private readonly IRebitStoreOperations _rebit;
    private readonly List<string> _cats = new List<string> { "Tom", "Garfield", "Sylvester" };
    private bool shouldDeleteResubmitionFile = Environment.GetEnvironmentVariable("AZREBIT_DELETE_RESUBMITION_FILE") == "true";
    public HttpCats(ILogger<HttpCats> logger,IRebitStoreOperations rebit)
    {
        _logger = logger;
        _rebit = rebit;
    }

    /// <summary>
    /// Http request trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("GetCats")]
    public async Task<HttpResponseData> RunGet([HttpTrigger(AuthorizationLevel.Anonymous, "get","post")]
    HttpRequestData req, FunctionContext funcContext)
    {
        var response = req.CreateResponse();
        response.StatusCode = System.Net.HttpStatusCode.OK;

        // ... some important work

        //cleanup
        //optional but recomended - if processing was successfull delete the file as we probably won't need it for resubmition to save storage space
        if (shouldDeleteResubmitionFile)
            await _rebit.DeleteResubmitFile(funcContext.InvocationId.ToString());

        await response.WriteAsJsonAsync(_cats);

        return response;
    }

}