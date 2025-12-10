using System;
using System.Collections.Generic;
using System.Text;

using AwesomeAssertions;

using AzRebit.Infrastructure;

using Azure.Storage.Blobs;

namespace AzRebit.IntegrationTests.BlobTriggerTest;

[Collection("FunctionApp")]
public class FunctionTriggeredByBlob
{
    [Fact]
    public async Task GivenAFunctionIsTriggeredByANewBlob_WhenABlobIsCreatedOrUpdated_ShouldSaveTheBlobAtResubmitLocation()
    {
        //arrange
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");
        var blobName = $"blob-{Guid.NewGuid}.txt";
        var inputBlobClient = new BlobClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"),"cats-container",blobName);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("A blob has been added");
        using var stream = new MemoryStream(data);
        //act
        var uploadResult=await inputBlobClient.UploadAsync(stream);
        //assert
        //check if we have the saved file in the ressubmition container
        uploadResult.Value.Should().NotBeNull();
        var resubmitionFileClient = new BlobClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), BlobResubmitStorage.IncomingFilesParentDirectory, blobName);
    }

}
