using System.Text;

using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;
using Microsoft.Extensions.Configuration;

namespace AzRebit.Features.RebitSave;

internal class RebitManualSave(IResubmitStorage storage) : IRebitManualSave
{
    public async Task<RebitActionResult> SavePayloadForResubmit(
        string payload, 
        string functionName,
        string id,
        string? fileName = null, 
        IDictionary<string, string>? destinationFileTags = null, 
        Encoding? encoding = null)
    {
        try
        {
            var resubmitContainerName=storage.RootSaveDirectory;
            (bool valid, string msg) = IsFunctionNameValid(functionName);
            if (!valid)
                return RebitActionResult.Failure(msg);

            string destinationPath = fileName != null
                ? $"{resubmitContainerName}/{functionName}/{fileName}"
                : GenerateDestinationName(functionName);
            await storage.SaveFileAtResubmitLocation(payload, destinationPath, id,destinationFileTags, encoding);
            return RebitActionResult.Success(destinationPath);
        }
        catch (Exception e)
        {
            return RebitActionResult.Failure(e.Message);
        }
    }

    public async Task<RebitActionResult> SavePayloadForResubmit(Stream payload, string functionName, string id,string? fileName = null, IDictionary<string, string>? destinationFileTags = null, Encoding? encoding = null)
    {
        try
        {
            (bool valid, string msg) = IsFunctionNameValid(functionName);
            if (!valid)
                return RebitActionResult.Failure(msg);

            string destinationPath = fileName != null
                ? $"{storage.RootSaveDirectory}/{functionName}/{fileName}"
                : GenerateDestinationName(functionName);
            await storage.SaveFileAtResubmitLocation(payload, destinationPath, id,destinationFileTags, encoding);
            return RebitActionResult.Success(destinationPath);
        }
        catch (Exception e)
        {
            return RebitActionResult.Failure(e.Message);
        }
    }

    private (bool, string) IsFunctionNameValid(string functionName)
    {
        if (functionName.Contains("/") || functionName.Contains(@"\"))
        {
            return (false, "Function name cannot be a path");
        }
        return (true, "");
    }

    private string GenerateDestinationName(string functionName)
        => $"{storage.RootSaveDirectory}/{functionName}/{Guid.NewGuid()}.txt";
}
