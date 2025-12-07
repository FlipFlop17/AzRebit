using System.Reflection;

using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.QueueTrigger.ResubmitHandler;
using AzRebit.Triggers.QueueTrigger.SaveRequestMiddleware;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Triggers.QueueTrigger;

internal class QueueFeatureSetup : ITriggerSetup
{
    public TriggerTypes.TriggerType TriggerSupport => TriggerTypes.TriggerType.Queue;

    public Type TriggerAttribute => typeof(QueueTriggerAttribute);

    public static AzFunction CreateAzFunction(string functionName, ParameterInfo parameter, IServiceCollection services)
    {
        var queueAttr = parameter.GetCustomAttribute<QueueTriggerAttribute>()!;
        var queueName = queueAttr.QueueName;
        var queueConnectionSettingName = queueAttr.Connection;
        var functionMeta = new Dictionary<string, string>();
        // Handle null/empty case
        if (string.IsNullOrEmpty(queueConnectionSettingName))
        {
            queueConnectionSettingName = "AzureWebJobsStorage";
        }
        // Handle custom names without prefix
        else if (!queueConnectionSettingName.StartsWith("AzureWebJobs"))
        {
            queueConnectionSettingName = $"AzureWebJobs{queueConnectionSettingName}";
        }
        var connectionString=Environment.GetEnvironmentVariable(queueConnectionSettingName);
        services.AddAzureClients(c => {
            c.AddQueueServiceClient(connectionString).WithName(functionName);
        });
        functionMeta.Add("QueueName", queueName);
        return new AzFunction(functionName,TriggerTypes.TriggerType.Queue,functionMeta);
    }

    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IResubmitHandler, StorageQueueResubmitHandler>();
        services.AddSingleton<IMiddlewareHandler, QueueMiddlewareHandler>();
        services.AddAzureClients(clients =>
        {
            clients.AddBlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!)
            .WithName(QueueMiddlewareHandler.ResubmitContainerNameName);
        });
    }
}