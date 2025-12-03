using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzRebit.HelperExtensions;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.FunctionExample.Features;

public class BlobCats
{
    private readonly ILogger<BlobCats> _logger;
    private bool deleteResubmitionFile = Environment.GetEnvironmentVariable("AZREBIT_DELETE_RESUBMITION_FILE") == "true";
    public BlobCats(ILogger<BlobCats> logger)
    {
        _logger = logger;
    }


    /// <summary>
    /// Blob trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("TransferCats")]
    public async Task RunCatTransfer(
        [BlobTrigger("cats-container/{blobPath}", Connection = "AzureWebJobsStorage")] 
        BlobClient blobClient, string blobPath,FunctionContext funcContext)
    {
        _logger.LogInformation("incoming payload saved");
        Console.WriteLine(blobClient.Name);
        //optional but recomended - if processing was successfull delete the file as we won't need it for resubmition
        if (deleteResubmitionFile)
            await AzRebitBlobExtensions.DeleteSavedResubmitionBlobAsync(funcContext.InvocationId.ToString());

        //publish the finished work on service bus, event, external endpoint etc.
    }
}
