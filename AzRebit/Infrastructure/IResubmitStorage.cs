using System.Text;

using Azure.Storage.Blobs;

namespace AzRebit.Infrastructure;

public interface IResubmitStorage
{
    public const string BlobTagInvocationId = "InvocationId";
    static string IncomingFilesParentDirectory=string.Empty;
    Task<BlobClient?> FindAsync(string invocationId);
    Task SaveFileAtResubmitLocation(BlobClient sourceBlob, string destinationFullPath, IDictionary<string, string>? destinationFileTags=default);
    Task SaveFileAtResubmitLocation(string payload, string destinationFullPath, IDictionary<string, string>? destinationFileTags = default, Encoding? encoding=default);
}
