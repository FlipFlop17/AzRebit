using System;
using System.Collections.Generic;
using System.Text;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzRebit.Tests;

internal static class TestHelpers
{

    /// <summary>
    /// Helper method to find the latest blob without using ambiguous extension methods.
    /// This resolves the OrderByDescending/FirstAsync ambiguity error.
    /// </summary>
    public static async Task<BlobItem?> GetLatestBlobAsync(this BlobContainerClient containerClient)
    {
        BlobItem? latestBlob = null;
        DateTimeOffset latestCreationTime = DateTimeOffset.MinValue;

        // Use efficient await foreach iteration
        await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(BlobTraits.Metadata))
        {
            if (blobItem.Properties.CreatedOn.HasValue && blobItem.Properties.CreatedOn.Value > latestCreationTime)
            {
                latestCreationTime = blobItem.Properties.CreatedOn.Value;
                latestBlob = blobItem;
            }
        }
        return latestBlob;
    }
}
