namespace AzRebit.Domain.Exceptions;

internal class BlobOperationException : Exception
{
    public string Operation { get; }
    public string Description { get; }

    public BlobOperationException(string operation, string message, Exception innerException)
        : base($"{message}", innerException)
    {
        Operation = operation;
        Description = message;
    }
}
