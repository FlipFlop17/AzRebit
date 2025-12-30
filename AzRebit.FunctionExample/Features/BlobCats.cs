using AzRebit.FunctionExample.Infra;
using AzRebit.Shared;
using AzRebit.Shared.Extensions;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.FunctionExample.Features;


public class Cat
{
    public string Name { get; set; }
    public string Color { get; set; }
}

public class BlobCats
{
    private readonly ILogger<BlobCats> _logger;
    private readonly IFunctionOutput _output;
    private bool deleteResubmitionFile = Environment.GetEnvironmentVariable("AZREBIT_DELETE_RESUBMITION_FILE") == "true";
    public BlobCats(ILogger<BlobCats> logger, IFunctionOutput output)
    {
        _logger = logger;
        _output = output;
    }


    /// <summary>
    /// Blob trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("TransferCats")]
    public async Task RunCatTransfer(
        [BlobTrigger("cats-container/{blobPath}", Connection = "AzureWebJobsStorage")]
        BlobClient blobClient, string blobPath, FunctionContext funcContext)
    {
        _logger.LogInformation("incoming payload saved");
        Console.WriteLine(blobClient.Name);
        //optional but recomended - if processing was successfull delete the file as we won't need it for resubmition
        if (deleteResubmitionFile)
            await AzRebitUtils.DeleteSavedResubmitionBlobAsync(funcContext.InvocationId.ToString());

        await _output.PostOutputAsync("Function processed-" + funcContext.InvocationId);
    }

    /// <summary>
    /// Blob trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("TransferCats-streambind")]
    public async Task RunCatTransferStream(
        [BlobTrigger("cats-container-stream/{blobName}", Connection = "AzureWebJobsStorage")]
        Stream blobStream, FunctionContext funcContext)
    {
        _logger.LogInformation("incoming payload saved");
        //optional but recomended - if processing was successfull delete the file as we won't need it for resubmition
        if (deleteResubmitionFile)
            await AzRebitUtils.DeleteSavedResubmitionBlobAsync(funcContext.InvocationId.ToString());

        await _output.PostOutputAsync("Function processed-" + funcContext.InvocationId);
    }

    /// <summary>
    /// Blob trigger example
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Function("TransferCats-custom")]
    public async Task RunCatTransferJson(
        [BlobTrigger("cats-container-custom/{blobObjectPath}", Connection = "AzureWebJobsStorage")]
        Cat catObject, FunctionContext funcContext)
    {
        _logger.LogInformation("incoming payload saved");
        //optional but recomended - if processing was successfull delete the file as we won't need it for resubmition
        if (deleteResubmitionFile)
            await AzRebitUtils.DeleteSavedResubmitionBlobAsync(funcContext.InvocationId.ToString());

        await _output.PostOutputAsync("Function processed-" + funcContext.InvocationId);
    }
}
