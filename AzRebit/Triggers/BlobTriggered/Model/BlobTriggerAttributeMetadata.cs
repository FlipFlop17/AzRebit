using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Triggers.BlobTriggered.Model;

internal class BlobTriggerAttributeMetadata
{
    public string? BlobPath { get; set; }
    /// <summary>
    /// Points to a environment variable that holds the connection string to the storage account
    /// </summary>
    public string? Connection { get; set; }
    public string? ContainerName { get; set; }
}
