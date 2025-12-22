using System.IO.Pipes;

using AzRebit.Domain.Enums;

using Azure;
using Azure.Data.Tables;

using Microsoft.Extensions.Azure;

namespace AzRebit.Infrastructure.StateStorage;


internal class StorageTablePersistService : IWorkItemStore
{
    private readonly TableClient _tableClient;

    public StorageTablePersistService(IAzureClientFactory<TableServiceClient> storage)
    {
        _tableClient = storage
            .CreateClient(ResubmitFunctionWorkerExtension.InternalRebitStorageTable)
            .GetTableClient("AzRebitStatePersist");
        _tableClient.CreateIfNotExists();
    }

    public async Task<bool> DeleteEntry(string invocationId,string functionName)
    {
        var isDeleted=await _tableClient.DeleteEntityAsync(functionName,invocationId);

        return !isDeleted.IsError;
    }

    public async Task<PagedResult<WorkItemEntity>> GetAsync(string? continuationToken)
    {
        var page = await _tableClient.QueryAsync<WorkItemEntity>()
                                  .AsPages(continuationToken)
                                  .FirstOrDefaultAsync();

        return new PagedResult<WorkItemEntity>
        {
            Items = page?.Values ?? [],
            NextToken = page?.ContinuationToken
        };
    }
    
    public async Task<bool> LogProcessingState(string invocationId,string filePath,string functionName,TriggerType triggerType)
    {
        var existingData = await GetEntityByInvocationId(invocationId);
        existingData?.RaiseResubmitCount();
        int newResubmitCount = 0;
        if (existingData is not null)
        {
            existingData.RaiseResubmitCount();
            newResubmitCount= existingData.ResubmitCount;
        }

        var data = new WorkItemEntity()
        {
            RowKey = invocationId,
            FilePath = filePath,
            PartitionKey = functionName,
            TriggerType = triggerType,
            ResubmitCount=newResubmitCount
        };

        var response=await _tableClient.UpsertEntityAsync(data);

        return !response.IsError;
    }
    public async Task<WorkItemEntity?> GetEntityByInvocationId (string invocationId)
    {
        var results = await _tableClient
            .QueryAsync<WorkItemEntity>(e => e.RowKey.Equals(invocationId))
            .FirstOrDefaultAsync();

        return results;
    }
}
