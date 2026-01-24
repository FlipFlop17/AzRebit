namespace AzRebit.Domain.Exceptions;

internal class BlobTagCountException : Exception
{
    public string Operation { get; }
    public string Description { get; }

    public BlobTagCountException(string operation, string message, Exception innerException)
        : base($"{message}", innerException)
    {
        Operation = operation;
        Description = message;
    }
}
