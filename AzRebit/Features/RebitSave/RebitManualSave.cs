using System.Text;

using AzRebit.Domain.Results;
using AzRebit.Infrastructure.FileStorage;

namespace AzRebit.Features.RebitSave;

internal class RebitManualSave : IRebitManualSave
{
    private readonly IResubmitStorage _storage;

    public RebitManualSave(IResubmitStorage storage)
    {
        _storage = storage;
    }

    public async Task<RebitActionResult> SavePayloadForResubmit(string payload, string functionName, string? fileName = null, IDictionary<string, string>? destinationFileTags = null, Encoding? encoding = null)
    {
        try
        {
            (bool valid, string msg) = IsFunctionNameValid(functionName);
            if (!valid)
                return RebitActionResult.Failure(msg);

            string destinationPath = fileName != null
                ? $"{BlobResubmitStorage.ResubmitContainerName}/{functionName}/{fileName}"
                : GenerateDestinationName(functionName);
            await _storage.SaveFileAtResubmitLocation(payload, destinationPath, destinationFileTags, encoding);
            return RebitActionResult.Success(destinationPath);
        }
        catch (Exception e)
        {
            return RebitActionResult.Failure(e.Message);
        }
    }

    public async Task<RebitActionResult> SavePayloadForResubmit(Stream payload, string functionName, string? fileName = null, IDictionary<string, string>? destinationFileTags = null, Encoding? encoding = null)
    {
        try
        {
            (bool valid, string msg) = IsFunctionNameValid(functionName);
            if (!valid)
                return RebitActionResult.Failure(msg);

            string destinationPath = fileName != null
                ? $"{BlobResubmitStorage.ResubmitContainerName}/{functionName}/{fileName}"
                : GenerateDestinationName(functionName);
            await _storage.SaveFileAtResubmitLocation(payload, destinationPath, destinationFileTags, encoding);
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
        => $"{BlobResubmitStorage.ResubmitContainerName}/{functionName}/{Guid.NewGuid()}.txt";
}
