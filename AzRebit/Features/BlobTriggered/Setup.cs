using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Enums;
using AzRebit.Shared;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Features.BlobTriggered;

/// <summary>
/// Setup is needed for the assembly discovery process to find and register this feature
/// </summary>
internal class Setup : TriggerSetupBase
{
    public override TriggerType TriggerName => TriggerType.Blob;
    public override Type TriggerAttribute => typeof(BlobTriggerAttribute);
    public override AzFunction TryCreateAzFunction(string functionName, TriggerBindingAttribute triggerAttribute, IServiceCollection services)
    {
        try
        {
            if (triggerAttribute is not BlobTriggerAttribute blobAttr) throw new Exception("Trigger binding attribute is null");

            var functionMeta = new Dictionary<string, string>();
            var connectionName =AssemblyDiscovery.ResolveConnectionStringAppSettingName(blobAttr.Connection);
            string connectionString = Environment.GetEnvironmentVariable(connectionName)!; //_config.GetValue<string>(connectionName)!;
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddBlobServiceClient(connectionString)
                    .WithName(functionName);
            });
            var azFunc= new AzFunction(functionName, TriggerType.Blob);
            azFunc.AddFunctionTriggerContainerName(BlobHelpers.ExtractContainerNameFromBlobPath(blobAttr.BlobPath));
            return azFunc;
        }
        catch (Exception e)
        {
            throw new AzFunctionNotCreatedException(e.Message, e);
        }
    }




}
