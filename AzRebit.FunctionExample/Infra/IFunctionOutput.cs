namespace AzRebit.FunctionExample.Infra;

public interface IFunctionOutput
{
    Task PostOutputAsync(string message);
}
