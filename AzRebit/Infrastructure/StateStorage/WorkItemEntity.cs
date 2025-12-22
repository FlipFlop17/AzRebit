using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzRebit.Domain.Enums;

using Azure;
using Azure.Data.Tables;

namespace AzRebit.Infrastructure.StateStorage;

internal class WorkItemEntity : ITableEntity
{
    /// <summary>
    /// represents the function name
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;
    /// <summary>
    /// represents the invocation id
    /// </summary>
    public string RowKey { get; set; }=string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public required string FilePath { get; init; }
    public required TriggerType TriggerType { get; init; }
    /// <summary>
    /// Shows how many times the specified file was resubmited
    /// </summary>
    public int ResubmitCount { get; set; } = 0;

    /// <summary>
    /// Iterates the resubmit count by 1 and returns the new value
    /// </summary>
    public int RaiseResubmitCount()
    {
        ResubmitCount++;
        return ResubmitCount;
    }
}
