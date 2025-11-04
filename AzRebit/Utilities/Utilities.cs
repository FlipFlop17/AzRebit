namespace AzRebit.Utilities;

internal static class Utility
{
    internal static class BlobHelpers
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
    
}
