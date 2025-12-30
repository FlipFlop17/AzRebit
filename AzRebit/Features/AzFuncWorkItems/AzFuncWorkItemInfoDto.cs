namespace AzRebit.Features.AzFuncWorkItems;

internal record AzFuncWorkItemInfoDto(string azureFunction, string invocationId, string fileName, int resubmitCount);
