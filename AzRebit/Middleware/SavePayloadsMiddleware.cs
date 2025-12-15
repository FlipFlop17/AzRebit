using System.ComponentModel;

using AzRebit.Shared;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace AzRebit.Middleware;

public record SavePayloadCommand(FunctionContext Context) : ISavePayloadCommand;


/// <summary>
/// Main entry middleware that will discover the trigger type and call the appropriate middleware handler to save the incoming request for resubmission.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public sealed class SavePayloadsMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<SavePayloadsMiddleware> _logger;
    private readonly IEnumerable<ISavePayloadHandler> _middlewareHandlers;
    public static EventId SkipAutoSave = new EventId(1000, "SkipAutoSave");
    public SavePayloadsMiddleware(ILogger<SavePayloadsMiddleware> logger,IEnumerable<ISavePayloadHandler> middlewareHandlers)
    {
        _logger = logger;
        _middlewareHandlers = middlewareHandlers;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            //skip if resubmit endpoint
            if (context.FunctionDefinition.Name.Equals("Resubmit", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(eventId:SkipAutoSave,"skiping payload saving for 'Resubmit' endpoint");
                await next(context);
                return;
            }
            var functionDefinition = context.FunctionDefinition;
            // Loop through all input bindings to find matching middleware handlers
            foreach (var binding in functionDefinition.InputBindings.Values)
            {
                // Find the appropriate middleware handler based on binding type
                var matchingHandler = _middlewareHandlers.FirstOrDefault(h =>
                    h.BindingName.Equals(binding.Type, StringComparison.OrdinalIgnoreCase));

                if (matchingHandler != null)
                {
                    _logger.LogDebug(
                        "Processing binding type '{BindingType}' with handler '{HandlerType}' for function '{FunctionName}'",
                        binding.Type,
                        matchingHandler.GetType().Name,
                        context.FunctionDefinition.Name);
                    
                    ISavePayloadCommand command = new SavePayloadCommand(context);
                    await matchingHandler.SaveIncomingRequest(command);
                    // Exit after finding and processing the first matching handler
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to auto-save blob for resubmission in function {FunctionName}",
                context.FunctionDefinition.Name);
            //we dont want to stop the function execution if the save fails. just log it.
        }

        await next(context);

    }
}
