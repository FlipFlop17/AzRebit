using System.Text;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace AzRebit.Infrastructure.FileStorage;

internal interface IResubmitStorage
{
    public const string BlobTagInvocationId = "InvocationId";
    static string IncomingFilesParentDirectory = string.Empty;
    Task<BlobClient?> FindAsync(string invocationId);
    Task SaveFileAtResubmitLocation(BlobBaseClient sourceBlob, string destinationFullPath, IDictionary<string, string>? destinationFileTags = default);
    Task SaveFileAtResubmitLocation(string payload, string destinationFullPath, IDictionary<string, string>? destinationFileTags = default, Encoding? encoding = default);
    Task SaveFileAtResubmitLocation(Stream payload, string destinationFullPath, IDictionary<string, string>? destinationFileTags = default, Encoding? encoding = default);
}
