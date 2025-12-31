using System.Text;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace AzRebit.Infrastructure.FileStorage;

internal interface IResubmitStorage
{
    string RootSaveDirectory {get;init;}
    Task<BlobClient?> FindAsync(string invocationId);
    Task SaveFileAtResubmitLocation(BlobBaseClient sourceBlob, string destinationFullPath,string id, IDictionary<string, string>? destinationFileTags = default);
    Task SaveFileAtResubmitLocation(string payload, string destinationFullPath, string id, IDictionary<string, string>? destinationFileTags = default, Encoding? encoding = default);
    Task SaveFileAtResubmitLocation(Stream payload, string destinationFullPath, string id,IDictionary<string, string>? destinationFileTags = default, Encoding? encoding = default);
}
