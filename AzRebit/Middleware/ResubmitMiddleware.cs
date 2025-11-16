using AzRebit.Shared;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace AzRebit.Middleware;

/// <summary>
/// Main entry middleware that will discover the trigger type and call the appropriate middleware handler to save the incoming request for resubmission.
/// </summary>
internal class ResubmitMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ResubmitMiddleware> _logger;
    private readonly IEnumerable<IMiddlewareHandler> _middlewareHandlers;

    public ResubmitMiddleware(ILogger<ResubmitMiddleware> logger,IEnumerable<IMiddlewareHandler> middlewareHandlers)
    {
        _logger = logger;
        _middlewareHandlers = middlewareHandlers;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            //skip if resubmit endpoint
            if (context.FunctionDefinition.Name.Equals("ResubmitHandler", StringComparison.OrdinalIgnoreCase))
            {
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

                    await matchingHandler.SaveIncomingRequest(context);
                    // Exit after finding and processing the first matching handler
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to auto-save blob for resubmission in function {FunctionName}",
                context.FunctionDefinition.Name);
            //we just log the warning, we dont want to stop the function execution if the save fails. just log it.
        }

        await next(context);
    }
}
