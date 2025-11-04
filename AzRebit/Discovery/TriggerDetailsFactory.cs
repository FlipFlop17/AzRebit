using System.Reflection;

using AzRebit.BlobTriggered.Model;
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
}
