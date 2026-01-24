namespace AzRebit.Domain.Exceptions;

/// <summary>
/// Different types of error occuring inside AzRebit lib.
/// </summary>
public enum AzRebitErrorType
{
    /// <summary>
    /// No error
    /// </summary>
    None,
    /// <summary>
    /// File needed for resubmition has not beend found
    /// </summary>
    BlobResubmitFileNotFound,
    /// <summary>
    /// Unexpected operation error
    /// </summary>
    UnexpectedError,
    /// <summary>
    /// Searched item was not found
    /// </summary>
    NotFound
}
