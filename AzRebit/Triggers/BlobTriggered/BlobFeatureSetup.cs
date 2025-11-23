using System.Reflection;

using AzRebit.Shared;
using AzRebit.Triggers.BlobTriggered.Handler;
using AzRebit.Triggers.BlobTriggered.Middleware;
using AzRebit.Triggers.BlobTriggered.Model;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.BlobTriggered;

/// <summary>
/// Setup is needed for the assembly discovery process to find and register this feature
/// </summary>
internal class BlobFeatureSetup:IFeatureSetup
{
    public TriggerType TriggerSupport => TriggerType.Blob;
    public Type TriggerAttribute => typeof(BlobTriggerAttribute);
    public object CreateTriggerMetadata(ParameterInfo parameter)
    {
        var blobAttr = parameter.GetCustomAttribute<BlobTriggerAttribute>()!;
        // Parse blob path to extract container and path pattern
        var blobPath = blobAttr.BlobPath ?? string.Empty;
        var pathParts = blobPath.Split('/', 2);
        var containerName = pathParts.Length > 0 ? pathParts[0] : string.Empty;
        var blobPathPattern = pathParts.Length > 1 ? pathParts[1] : string.Empty;

        return new BlobTriggerAttributeMetadata
        {
            ContainerName = containerName,
            BlobPath = blobPathPattern,
            Connection = blobAttr.Connection
        };
    }

    public static void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IResubmitHandler, BlobResubmitHandler>();
        services.AddSingleton<IMiddlewareHandler, BlobMiddlewareHandler>();
        services.AddAzureClients(clients =>
        {
            clients.AddBlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!)
            .WithName(BlobMiddlewareHandler.BlobResubmitContainerName);
        });
    }


}
