internal class AzFunctionNotCreatedException:Exception
{
    public override string Message { get; }

    public AzFunctionNotCreatedException(string message, Exception innerException)
        : base($"{message}", innerException)
    {
        Message = message;
    }
}