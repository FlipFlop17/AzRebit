using AzRebit.Shared;
using AzRebit.Shared.Model;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static AzRebit.ResubmitFunctionWorkerExtension;

namespace AzRebit.Endpoints.Resubmit;

public class ResubmitResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = "Resubmit succeeded";
    public object? InvokedFunctionResponse { get; set; }
}


internal class ResubmitEndpoint
{
    private readonly IReadOnlyCollection<AzFunction> _availableFunctions;
    private readonly IEnumerable<IResubmitHandler> _triggerHandlers;
    private readonly ILogger<ResubmitEndpoint> _logger;
    public ResubmitEndpoint(
        IOptions<ResubmitOptions> options,
        IReadOnlyCollection<AzFunction> functionNames,
        IEnumerable<IResubmitHandler> triggerHandlers,
        ILogger<ResubmitEndpoint> logger)
    {
        _availableFunctions = functionNames;
        _triggerHandlers = triggerHandlers;
        _logger = logger;

    }


    //TODO:Do we need an endpoint that will fetch all resubmitions done. Maybe track them in a storage table. last 3 days ?? 

    [Function("Resubmit")]
    public async Task<HttpResponseData> RunResubmit(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resubmit")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var resubmitResult = new ResubmitResponse();
        // Extract query parameters
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var functionName = query["functionName"];
        var invocationId = query["invocationId"];

        if(!ValidateRequest(functionName, invocationId))
        {
            var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            resubmitResult.IsSuccess = false;
            resubmitResult.Message = "Invalid request parameters";
            await badRequestResponse.WriteAsJsonAsync(resubmitResult);
            return badRequestResponse;
        }

        try
        {
            _logger.LogInformation("Resubmit request received for function: {FunctionName} with invocationId: {InvocationId}", functionName, invocationId);

            var handlerResult=await HandleResubmit(functionName!, invocationId!);
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);

            if (!handlerResult.IsSuccess)
            {
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                resubmitResult.IsSuccess = false;
                resubmitResult.Message = handlerResult.Message ?? "Resubmit success";
            }
            
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
        IResubmitHandler handler = _triggerHandlers.FirstOrDefault(h =>
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
