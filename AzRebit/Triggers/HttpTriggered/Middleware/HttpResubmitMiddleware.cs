using AzRebit.Extensions;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace AzRebit.Triggers.HttpTriggered.Middleware;

internal class HttpResubmitMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<HttpResubmitMiddleware> _logger;

    public HttpResubmitMiddleware(ILogger<HttpResubmitMiddleware> logger)
    {
        _logger = logger;
    }
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            // Check if this is an HTTP trigger function
            var functionDefinition = context.FunctionDefinition;
            var httpTriggerBinding = functionDefinition.InputBindings
                .FirstOrDefault(b => b.Value.Type.Equals("httpTrigger", StringComparison.OrdinalIgnoreCase));

            if (httpTriggerBinding.Value != null)
            {
                // Get the HttpRequestData from the invocation
                var httpRequestData = await context.GetHttpRequestDataAsync();

                if (httpRequestData != null)
                {
                    string invocationId = context.InvocationId;

                    _logger.LogInformation(
                        "Auto-saving HTTP request for resubmission with invocationId: {invocationId}",
                        invocationId);

                    // Automatically save the request for resubmission
                    await httpRequestData.SaveRequestForResubmitionAsync(invocationId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to auto-save HTTP request for resubmission in function {FunctionName}",
                context.FunctionDefinition.Name);
            // Just log the warning, don't stop function execution if save fails
        }

        await next(context);
    }
}
