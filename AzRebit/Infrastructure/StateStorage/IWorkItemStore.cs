using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzRebit.Domain.Enums;

using Azure;

namespace AzRebit.Infrastructure.StateStorage;

internal class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public string? NextToken { get; set; }
}
internal interface IWorkItemStore
{
    /// <summary>
    /// Logs the current state of a processing request
    /// </summary>
    /// <returns>
    /// <strong>true</strong> if its a success or <strong>false</strong> if not
    /// </returns>
    /// <param name="filePath">full file path of the file</param>
    /// <param name="functionName">name of the function the file belongs to</param>
    /// <param name="invocationId">the invocation id of the run</param>
    /// <param name="triggerType">the trigger type on the function</param>
    public Task<bool> LogProcessingState(string invocationId, string filePath, string functionName, TriggerType triggerType);
    /// <summary>
    /// Gets all the file saved for resubmition
    /// </summary>
    /// <returns></returns>
    public Task<PagedResult<WorkItemEntity>> GetAsync(string? continuationToken);

    public Task<bool> DeleteEntry(string invocationId,string functionName);
    Task<WorkItemEntity?> GetEntityByInvocationId(string invocationId);

}
