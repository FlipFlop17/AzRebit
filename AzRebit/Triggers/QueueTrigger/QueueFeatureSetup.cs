using System.Reflection;

using AzRebit.Shared;
using AzRebit.Shared.Model;
using AzRebit.Triggers.QueueTrigger.ResubmitHandler;
using AzRebit.Triggers.QueueTrigger.SaveRequestMiddleware;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Triggers.QueueTrigger;

internal class QueueFeatureSetup : IFeatureSetup
{
    public TriggerTypes.TriggerType TriggerSupport => TriggerTypes.TriggerType.Queue;

    public Type TriggerAttribute => typeof(QueueTriggerAttribute);

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

    public object? CreateTriggerMetadata(ParameterInfo parameter)
    {
        var queueAttr = parameter.GetCustomAttribute<QueueTriggerAttribute>()!;
        var queueName = queueAttr.QueueName;
        var queueConnectionSettingName = queueAttr.Connection;
        var queueMeta = new QueueTriggerAttributeMetadata();

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

        // If it's already "AzureWebJobsSomething", it stays as-is
        return new QueueTriggerAttributeMetadata
        {
            QueueName = queueName,
            ConnectionString = Environment.GetEnvironmentVariable(queueConnectionSettingName) ?? string.Empty
        };
    }
}

internal sealed class QueueTriggerAttributeMetadata
{
    public string QueueName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}