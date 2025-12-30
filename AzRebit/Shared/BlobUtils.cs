namespace AzRebit.Shared;

/// <summary>
/// Provides helper methods for working with blob storage paths.
/// </summary>
/// <remarks>This class contains static utility methods for parsing and extracting information from blob storage
/// paths, such as those used in Azure Blob Storage. All members are static and the class cannot be
/// instantiated.</remarks>
public static class BlobHelpers
{
    /// <summary>
    /// Extracts the container name from a blob path.
    /// </summary>
    /// <param name="blobPath"></param>
    /// <returns></returns>
    public static string ExtractContainerNameFromBlobPath(string blobPath)
    {
        if (string.IsNullOrEmpty(blobPath))
            return string.Empty;

        // Blob path format: "container-name/path/to/blob"
        var parts = blobPath.Split('/');
        return parts.Length > 0 ? parts[0] : string.Empty;
    }
}


