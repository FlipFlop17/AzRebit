using AzRebit.Shared;
using AzRebit.Shared.Model;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Triggers.BlobTriggered;

/// <summary>
/// Setup is needed for the assembly discovery process to find and register this feature
/// </summary>
internal class Setup : TriggerSetupBase
{
    public override TriggerName TriggerName => TriggerName.Blob;
    public override Type TriggerAttribute => typeof(BlobTriggerAttribute);

    public override AzFunction TryCreateAzFunction(string functionName, TriggerBindingAttribute triggerAttribute, IServiceCollection services)
    {
        try
        {
            if (triggerAttribute is not BlobTriggerAttribute blobAttr) throw new Exception("Trigger binding attribute is null");

            var functionMeta = new Dictionary<string, string>();
            var connectionName =AssemblyDiscovery.ResolveConnectionStringAppSettingName(blobAttr.Connection);
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddBlobServiceClient(connectionName)
                    .WithName(functionName);
            });

            return new AzFunction(functionName, TriggerName.Blob, functionMeta);
        }
        catch (Exception e)
        {
            throw new AzFunctionNotCreatedException(e.Message, e);
        }
    }




}
