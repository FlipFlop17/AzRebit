using System.Net;

using AwesomeAssertions;

using AzRebit.Triggers.BlobTriggered.Handler;

using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Mvc.Testing;

namespace AzFunctionResubmit.Tests.IntegrationTests;

[TestClass]
public class GetCatsTests
{
    private const string AzuriteConnectionString = "UseDevelopmentStorage=true";
    private  static WebApplicationFactory<Program> _programInstance;

    [ClassInitialize]
    public void ClassInit()
    {
        // Set the connection string for the tests
        _programInstance=new WebApplicationFactory<Program>();
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", AzuriteConnectionString);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        // Clean up test blobs after each test
        try
        {
            var containerClient = new BlobContainerClient(
                AzuriteConnectionString, 
               BlobResubmitHandler.BlobResubmitContainerName);
            
            if (await containerClient.ExistsAsync())
            {
                await foreach (var blob in containerClient.GetBlobsAsync())
                {
                    await containerClient.DeleteBlobIfExistsAsync(blob.Name);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }


    [TestMethod]
    [DataRow("/GetCats")]
    public async Task GetCats_RunGet_ShouldReturnsSuccessResponse(string endpoint)
    {
        // Arrange
        var httpClient = _programInstance.CreateClient();
        // Act
        var functionResponse=await httpClient.GetAsync(endpoint);

        // Assert
        functionResponse.Should().Be(HttpStatusCode.OK);
    }


}
