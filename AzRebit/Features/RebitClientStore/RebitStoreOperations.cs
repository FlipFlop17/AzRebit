using System.Text;

using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;

namespace AzRebit.Features.RebitClientStore;

/// <summary>
/// Client exposed operations for file management
/// </summary>
/// <param name="storage"></param>
internal class RebitStoreOperations(IResubmitStorage storage) : IRebitStoreOperations
{

    public async Task<RebitResult> DeleteResubmitFile(string id)
    {
        try
        {
            bool isDeleted = await storage.DeleteFile(id);
            return RebitResult.Success($"deleted: {isDeleted}");
        }
        catch (Exception e)
        {
            return RebitResult.Failure(e.Message);
        }
    }

    public async Task<RebitResult> SavePayloadForResubmit(
        string payload,
        string functionName,
        string id,
        string? fileName = null,
        IDictionary<string, string>? destinationFileTags = null,
        Encoding? encoding = null)
    {
        try
        {
            (bool valid, string msg) = IsFunctionNameValid(functionName);
            if (!valid)
                return RebitResult.Failure(msg);

            string destinationPath = fileName != null
                ? $"{functionName}/{fileName}"
                : GenerateDestinationName(functionName);
            await storage.SaveFileAtResubmitLocation(payload, destinationPath, id, destinationFileTags, encoding);
            return RebitResult.Success(destinationPath);
        }
        catch (Exception e)
        {
            return RebitResult.Failure(e.Message);
        }
    }

    public async Task<RebitResult> SavePayloadForResubmit(
        Stream payload,
        string functionName,
        string id,
        string? fileName = null,
        IDictionary<string, string>? destinationFileTags = null,
        Encoding? encoding = null)
    {
        try
        {
            (bool valid, string msg) = IsFunctionNameValid(functionName);
            if (!valid)
                return RebitResult.Failure(msg);

            string destinationPath = fileName != null
                ? $"{functionName}/{fileName}"
                : GenerateDestinationName(functionName);
            await storage.SaveFileAtResubmitLocation(payload, destinationPath, id, destinationFileTags, encoding);
            return RebitResult.Success(destinationPath);
        }
        catch (Exception e)
        {
            return RebitResult.Failure(e.Message);
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
        => $"{functionName}/{Guid.NewGuid()}.txt";
}
