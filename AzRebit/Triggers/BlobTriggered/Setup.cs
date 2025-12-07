using System.Reflection;

using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.BlobTriggered.Handler;
using AzRebit.Triggers.BlobTriggered.Middleware;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.BlobTriggered;

/// <summary>
/// Setup is needed for the assembly discovery process to find and register this feature
/// </summary>
internal class Setup:TriggerSetupBase
{
    public override TriggerType TriggerSupport => TriggerType.Blob;
    public override Type TriggerAttribute => typeof(BlobTriggerAttribute);
    public override AzFunction CreateAzFunction(string functionName,  ParameterInfo parameter, IServiceCollection services)
    {
        var blobAttr = parameter.GetCustomAttribute<BlobTriggerAttribute>()!;
        var functionMeta = new Dictionary<string, string>();
        // Parse blob path to extract container and path pattern
        var blobPath = blobAttr.BlobPath ?? string.Empty;
        var pathParts = blobPath.Split('/', 2);
        var containerName = pathParts.Length > 0 ? pathParts[0] : string.Empty;
        var blobPathPattern = pathParts.Length > 1 ? pathParts[1] : string.Empty;
        services.AddAzureClients(c => {
            c.AddBlobServiceClient(blobAttr.Connection).WithName(functionName);
        });

        return new AzFunction(functionName,TriggerType.Blob, functionMeta);
    }

   

}
