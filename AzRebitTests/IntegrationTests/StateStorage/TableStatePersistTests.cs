using System.Text.Json;

using AwesomeAssertions;

using AzRebit.Domain.Enums;
using AzRebit.Infrastructure.StateStorage;

using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;

namespace AzRebitTests.IntegrationTests.StateStorage;


[Collection("FunctionApp")]
public class ResubmitStateTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IWorkItemStore _stateTableStorage;

    public ResubmitStateTests(FunctionAppFixture host, ITestOutputHelper output)
    {
        _output = output;
        //_inputQueueClient = host.ServiceProvider
        //   .GetRequiredService<IAzureClientFactory<QueueServiceClient>>()
        //   .CreateClient("queueClient");
        //_blobResubmitContainerClient = host.ServiceProvider
        //    .GetRequiredService<IAzureClientFactory<BlobServiceClient>>()
        //    .CreateClient("resubmitContainer")
        //    .GetBlobContainerClient("files-for-resubmit");
        _stateTableStorage = host.ServiceProvider.GetRequiredService<IWorkItemStore>();
    }

    public void Dispose()
    {

    }

    [Fact]
    public async Task Should_Save_File_State_To_Storage()
    {
        //arrange
        string invocationId = "21222222";
        //act

        bool list = await _stateTableStorage.LogProcessingState(invocationId, "/blobdata/test.txt", "TransferCats", TriggerType.Blob);

        //assert
        list.Should().BeTrue();
        var dataInserted = await _stateTableStorage.GetEntityByInvocationId(invocationId);
        dataInserted.Should().NotBeNull();
        dataInserted.RowKey.Should().Be(invocationId);

        var allData = await _stateTableStorage.GetAsync(string.Empty);
        foreach (var item in allData.Items)
        {
            _output.WriteLine(JsonSerializer.Serialize(item));
        }
    }

}
