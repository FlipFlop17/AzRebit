using System.ComponentModel;

using AzRebit.Domain.Abstractions;
using AzRebit.Shared.Extensions;

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
                // we need to find the handler for this type of request
                var matchingHandler = _middlewareHandlers.FirstOrDefault(h =>
                    h.BindingName.Equals(binding.Type, StringComparison.OrdinalIgnoreCase));

                if (matchingHandler != null)
                {
                    _logger.LogMiddlewareProcessing(context.InvocationId,context.FunctionDefinition.Name);
                    
                    ISavePayloadCommand command = new SavePayloadCommand(context);
                    var handlerResult=await matchingHandler.SaveIncomingRequest(command);

                    _logger.LogMiddlewareFinished(context.InvocationId, context.FunctionDefinition.Name,handlerResult.IsSuccess,handlerResult.Message);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogMiddlewareError(ex,context.InvocationId, context.FunctionDefinition.Name);
            //we dont want to stop the function execution if the save fails. just log it.
        }

        await next(context);

    }
}
