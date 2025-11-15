using AzRebit.Shared;
using AzRebit.Shared.Model;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static AzRebit.ResubmitFunctionWorkerExtension;

namespace AzRebit.Endpoints.Resubmit;

internal class ResubmitEndpoint
{
    private readonly IReadOnlyCollection<AzFunction> _availableFunctions;
    private readonly IEnumerable<ITriggerHandler> _triggerHandlers;
    private readonly ILogger<ResubmitEndpoint> _logger;
    public ResubmitEndpoint(
        IOptions<ResubmitOptions> options,
        IReadOnlyCollection<AzFunction> functionNames,
        IEnumerable<ITriggerHandler> triggerHandlers,
        ILogger<ResubmitEndpoint> logger)
    {
        _availableFunctions = functionNames;
        _triggerHandlers = triggerHandlers;
        _logger = logger;

    }

    [Function("ResubmitHandler")]
    public async Task<HttpResponseData> RunResubmit(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resubmit")] HttpRequestData req,
        FunctionContext executionContext)
    {

        // Extract query parameters
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var functionName = query["functionName"];
        var invocationId = query["invocationId"];

        if(!ValidateRequest(functionName, invocationId))
        {
            var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new { Error = "Invalid request parameters" });
            return badRequestResponse;
        }

        try
        {
            _logger.LogInformation("Resubmit request received for function: {FunctionName} with invocationId: {InvocationId}", functionName, invocationId);

            var resubmitResult=await HandleResubmit(functionName!, invocationId!);
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);

            if (!resubmitResult.IsSuccess)
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
            
            await response.WriteAsJsonAsync(new
            {
                resubmitResult.IsSuccess,
                resubmitResult.Message,
                FunctionName=functionName,
                InvocationId=invocationId,
                Timestamp = DateTime.UtcNow
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing resubmit request for function: {FunctionName} with invocationId: {InvocationId}", functionName, invocationId);
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { Error = "Internal server error" });
            return errorResponse;
        }
    }

    private async Task<ActionResult> HandleResubmit(string functionName,string invocationId)
    {
        var functionForResubmit = _availableFunctions.First(fn => fn.Name.Equals(functionName));
        var functionsTriggerMetadata = functionForResubmit.TriggerMetadata;
        //find the handler for this type of trigger
        ITriggerHandler handler = _triggerHandlers.FirstOrDefault(h =>
        {
            return h.HandlerType == functionForResubmit.TriggerType;
        }) ?? throw new InvalidOperationException($"No trigger handler found for function '{functionName}' with the trigger type {functionForResubmit.TriggerType}");

        var handlerResponse= await handler.HandleResubmitAsync(invocationId, functionsTriggerMetadata);

        return handlerResponse;
    }

    private bool ValidateRequest(string? functionName, string? invocationId)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(functionName))
        {
            _logger.LogWarning("Validation failed: Missing required query parameter: functionName");
            return false;
        }

        if (string.IsNullOrWhiteSpace(invocationId))
        {
            _logger.LogWarning("Validation failed: Missing required query parameter: invocationId");
            return false;
        }

        // Validate function exists
        if (!_availableFunctions.Any(fn => fn.Name.Equals(functionName)))
        {
            _logger.LogWarning("Validation failed: Function '{FunctionName}' not found", functionName);
            return false;
        }

        return true;
    }


}
