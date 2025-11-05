using System.Reflection;

using AzRebit.Triggers.BlobTriggered.Model;
using AzRebit.Triggers.HttpTriggered.Model;
using AzRebit.Utilities;

using Microsoft.Azure.Functions.Worker;

using static AzRebit.Shared.Model.TriggerTypes;

namespace AzRebit.Discovery;

internal static class TriggerDetailsFactory
{

    /// <summary>
    /// Factory method to create TriggerDetails object based on TriggerType
    /// </summary>
    /// <param name="type"></param>
    /// <param name="attributeParamInfo"></param>
    /// <returns>object</returns>
    internal static object? CreateTriggerDetails(TriggerType type, ParameterInfo attributeParamInfo)
    {
        switch (type)
        {
            case TriggerType.Blob:
                var triggerDetails = CreateBlobTriggerDetails(attributeParamInfo);
                //add more trigger types here in future
                return triggerDetails;
            case TriggerType.Http:
                var triggerDetailsHttp = CreateHttpTriggerDetails(attributeParamInfo);
                return triggerDetailsHttp;
            default:
                return null;
        }
    }

    private static BlobTriggerDetails CreateBlobTriggerDetails(ParameterInfo paramData)
    {
        BlobTriggerAttribute blobFuncTriggerInfo = paramData.GetCustomAttribute<BlobTriggerAttribute>()!;
        var triggerDetails = new BlobTriggerDetails();
        triggerDetails.TypeOfTriger = TriggerType.Blob;
        triggerDetails.ConnectionString = blobFuncTriggerInfo.Connection;
        triggerDetails.ContainerName = Utility.BlobHelpers.ExtractContainerNameFromBlobPath(blobFuncTriggerInfo.BlobPath);
        triggerDetails.TriggerPath = blobFuncTriggerInfo.BlobPath;
        return triggerDetails;
    }
    private static HttpTriggerDetails CreateHttpTriggerDetails(ParameterInfo paramData)
    {
        HttpTriggerAttribute httpFuncTriggerInfo = paramData.GetCustomAttribute<HttpTriggerAttribute>()!;
        var triggerDetails = new HttpTriggerDetails();
        triggerDetails.Route = httpFuncTriggerInfo.Route ?? string.Empty;
        return triggerDetails;
    }
}
