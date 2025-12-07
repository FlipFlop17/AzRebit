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
internal class Setup : TriggerSetupBase
{
    public override TriggerType TriggerSupport => TriggerType.Blob;

    public override AzFunction TryCreateAzFunction(string functionName, ParameterInfo[] parameters, IServiceCollection services)
    {
        BlobTriggerAttribute? blobAttr;

        try
        {
            var functionsTriggerParameter = parameters.FirstOrDefault(
                p => p.GetCustomAttribute<BlobTriggerAttribute>() != null
            );

            if (functionsTriggerParameter is null)
                throw new ArgumentNullException(nameof(functionsTriggerParameter));

            blobAttr = functionsTriggerParameter.GetCustomAttribute<BlobTriggerAttribute>();

            if (blobAttr is null)
                throw new ArgumentNullException(nameof(blobAttr));
        }
        catch (ArgumentNullException e)
        {
            throw new AzFunctionNotCreatedException(e.Message, e);
        }

        try
        {
            var functionMeta = new Dictionary<string, string>();
            var connectionName =AssemblyDiscovery.ResolveConnectionString(blobAttr.Connection);
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddBlobServiceClient(connectionName)
                    .WithName(functionName);
            });

            return new AzFunction(functionName, TriggerType.Blob, functionMeta);
        }
        catch (Exception e)
        {
            throw new AzFunctionNotCreatedException(e.Message, e);
        }
    }




}
