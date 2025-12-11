using System.Net;
using System.Reflection;

using AzRebit.Model;
using AzRebit.Shared;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static System.Runtime.InteropServices.JavaScript.JSType;
using static AzRebit.ResubmitFunctionWorkerExtension;

namespace AzRebit.Endpoints.Resubmit;

public record ResubmitResponse(bool IsSuccess,string Message, string? FunctionName=null,string? InvocationId=null);


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

    /// <summary>
    /// Resubmits a specified file or a request by creating a new request to the specified function. Via http or blob or queueu
    /// </summary>
    /// <param name="req"></param>
    /// <param name="executionContext"></param>
    /// <returns>ResubmitResponse</returns>
    [Function("Resubmit")]
    public async Task<HttpResponseData> RunResubmit(
        [HttpTrigger(AuthorizationLevel.Anonymous, ["get"], Route = "resubmit")] HttpRequestData req,
        FunctionContext executionContext)
    {
        // Extract query parameters
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var functionName = query["functionName"];
        var invocationIdToResubmit = query["invocationId"];
        (bool isValid, string msg) = ValidateRequest(functionName,invocationIdToResubmit);
        if(!isValid)
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            var resubmitResult = new ResubmitResponse(false,msg,functionName,invocationIdToResubmit);
            await badRequestResponse.WriteAsJsonAsync(resubmitResult);
            return badRequestResponse;
        }

        try
        {
            _logger.LogInformation("Resubmit request received for function: {FunctionName} with invocationId: {InvocationId}", functionName, invocationIdToResubmit);

            var handlerResult=await HandleResubmit(functionName!, invocationIdToResubmit!);
            var response = req.CreateResponse(HttpStatusCode.OK);

            if (!handlerResult.IsSuccess)
            {
                response.StatusCode = handlerResult.ErrorType switch
                {
                   AzRebitErrorType.BlobResubmitFileNotFound => HttpStatusCode.NotFound,
                   _=>HttpStatusCode.InternalServerError
                };
                string handlerMsg = handlerResult.Message ?? "Resubmit success";
                var resubmitFailResult = new ResubmitResponse(false, msg,functionName,invocationIdToResubmit);
                await response.WriteAsJsonAsync(resubmitFailResult);
                return response;
            }
            //TODO: Ne vrati novi invocation id, treba bi vratiti novi invocation id - stari ionako user napravi u requestu
            var resubmitResult = new ResubmitResponse(true, "Sucessful resubmition", functionName, invocationIdToResubmit);
            await response.WriteAsJsonAsync(resubmitResult);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing resubmit request for function: {FunctionName} with invocationId: {InvocationId}", functionName, invocationIdToResubmit);
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { Error = "Internal server error" });
            return errorResponse;
        }
    }

    private async Task<RebitActionResult> HandleResubmit(string functionName,string invocationId)
    {
        var functionForResubmit = _availableFunctions.First(fn => fn.Name.Equals(functionName));
        var functionsTriggerMetadata = functionForResubmit.TriggerMetadata;
        //find the handler for this type of trigger
        IResubmitHandler handler = _triggerHandlers.FirstOrDefault(h =>
        {
            return h.HandlerType == functionForResubmit.TriggerType;
        }) ?? throw new InvalidOperationException($"No trigger handler found for function '{functionName}' with the trigger type {functionForResubmit.TriggerType}");

        var handlerResponse= await handler.HandleResubmitAsync(invocationId, functionForResubmit);

        return handlerResponse;
    }

    private (bool,string) ValidateRequest(string? functionName, string? invocationId)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(functionName))
        {
            _logger.LogWarning("Validation failed: Missing required query parameter: functionName");
            return (false, "Missing required query parameter: functionName");
        }

        if (string.IsNullOrWhiteSpace(invocationId))
        {
            _logger.LogWarning("Validation failed: Missing required query parameter: invocationId");
            return (false, "Missing required query parameter: invocationId");
        }

        // Validate function exists
        if (!_availableFunctions.Any(fn => fn.Name.Equals(functionName)))
        {
            _logger.LogWarning("Validation failed: Function '{FunctionName}' not found", functionName);
            return (false, $"Function '{functionName}' not found");
        }

        return (true,string.Empty);
    }


}
