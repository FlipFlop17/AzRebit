using System.Net;

using AzRebit.Domain.Abstractions;
using AzRebit.Domain.Entities;
using AzRebit.Domain.Exceptions;
using AzRebit.Domain.Results;
using AzRebit.Shared.Extensions;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static System.Runtime.InteropServices.JavaScript.JSType;
using static AzRebit.ResubmitFunctionWorkerExtension;

namespace AzRebit.Features.Resubmit;

/// <summary>
/// Represents the result of a resubmission operation, including status, message, and related metadata.
/// </summary>
/// <param name="IsSuccess">true if the resubmission was successful; otherwise, false.</param>
/// <param name="Message">A message describing the outcome of the resubmission operation.</param>
/// <param name="FunctionName">The name of the function associated with the resubmission, or null if not applicable.</param>
/// <param name="ResubmitedFileName">The name of the file that was resubmitted, or null if not applicable.</param>
public record ResubmitResponse(bool IsSuccess, string Message, string? FunctionName = null, string? ResubmitedFileName = null);

internal class ResubmitEndpoint
{
    private readonly IReadOnlyCollection<AzFunction> _availableFunctions;
    private readonly IEnumerable<IResubmitHandler> _triggerHandlers;
    private readonly ILogger<ResubmitEndpoint> _logger;
    public ResubmitEndpoint(
        IReadOnlyCollection<AzFunction> functionNames,
        IEnumerable<IResubmitHandler> triggerHandlers,
        ILogger<ResubmitEndpoint> logger)
    {
        _availableFunctions = functionNames;
        _triggerHandlers = triggerHandlers;
        _logger = logger;

    }

    /// <summary>
    /// Resubmits a specified file or a request by creating a new request to the specified function. Via http or blob or queueu
    /// </summary>
    /// <param name="req"></param>
    /// <param name="executionContext"></param>
    /// <returns>ResubmitResponse</returns>
    [Function("Resubmit")]
    public async Task<HttpResponseData> RunResubmit(
        [HttpTrigger(AuthorizationLevel.Anonymous, ["get"], Route = "azrebit/resubmit")] HttpRequestData req,
        FunctionContext executionContext)
    {
        // Extract query parameters
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var functionName = query["functionName"];
        var invocationIdToResubmit = query["invocationId"];
        (bool isValid, string msg) = ValidateRequest(functionName, invocationIdToResubmit);
        if (!isValid)
        {
            _logger.LogValidationError(msg);
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            var resubmitResult = new ResubmitResponse(false, msg, functionName, invocationIdToResubmit);
            await badRequestResponse.WriteAsJsonAsync(resubmitResult);
            return badRequestResponse;
        }
        string validFunctionName = functionName!;
        string validInvocationId = invocationIdToResubmit!;
        // ako zovemo endpoint preko httpa onda vrati response accepted i caller moze staviti status 'resubmit sent at'.
        //ako iz azure workbooka zovcemo caller isto moze defoltno stavit status resubmit sent at. downside workbooka je sta nemremo provjheriti /resubmit response pa ako je rtesponse 404npr nemremo staviti to.
        //

        try
        {
            _logger.LogResubmitStart(validInvocationId, validFunctionName);

            var handlerResult = await HandleResubmit(validFunctionName, validInvocationId);
            var response = req.CreateResponse(HttpStatusCode.OK);

            if (!handlerResult.IsSuccess)
            {
                response.StatusCode = handlerResult.ErrorType switch
                {
                    AzRebitErrorType.BlobResubmitFileNotFound => HttpStatusCode.NotFound,
                    _ => HttpStatusCode.InternalServerError
                };
                string handlerMsg = handlerResult.Message ?? "Resubmit is not successfull";
                var resubmitFailResult = new ResubmitResponse(false, handlerMsg, validFunctionName, validInvocationId);
                _logger.LogResubmitStatus(validInvocationId, validFunctionName, handlerResult.IsSuccess, handlerMsg);
                await response.WriteAsJsonAsync(resubmitFailResult);
                return response;
            }

            var resubmitResult = new ResubmitResponse(true, "Sucessful resubmition", validFunctionName, handlerResult.Data?.ResubmitingFileName);
            _logger.LogResubmitStatus(validInvocationId, validFunctionName, handlerResult.IsSuccess, null);
            await response.WriteAsJsonAsync(resubmitResult);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error processing resubmit request for function: {FunctionName} with invocationId: {InvocationId}", validFunctionName, validInvocationId);
            _logger.LogResubmitError(ex,
                validInvocationId,
                validFunctionName);
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { Error = ex.Message });
            return errorResponse;
        }
    }

    private async Task<RebitActionResult<ResubmitHandlerResponse>> HandleResubmit(string functionName, string invocationId)
    {
        var functionForResubmit = _availableFunctions.First(fn => fn.Name.Equals(functionName)) 
            ?? throw new InvalidOperationException($"Function {functionName} not available");
        
        var functionsTriggerMetadata = functionForResubmit.TriggerMetadata;
        //find the handler for this type of trigger
        IResubmitHandler handler = _triggerHandlers.FirstOrDefault(h =>
        {
            return h.HandlerType == functionForResubmit.TriggerType;
        }) ?? throw new InvalidOperationException($"No trigger handler found for function '{functionName}' with the trigger type {functionForResubmit.TriggerType}");

        var handlerResponse = await handler.HandleResubmitAsync(invocationId, functionForResubmit);

        return handlerResponse;
    }

    private (bool, string) ValidateRequest(string? functionName, string? invocationId)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(functionName))
        {
            _logger.LogDebug("Validation failed: Missing required query parameter: functionName");
            return (false, "Missing required query parameter: functionName");
        }

        if (string.IsNullOrWhiteSpace(invocationId))
        {
            _logger.LogDebug("Validation failed: Missing required query parameter: invocationId");
            return (false, "Missing required query parameter: invocationId");
        }

        // Validate function exists
        if (!_availableFunctions.Any(fn => fn.Name.Equals(functionName)))
        {
            _logger.LogDebug("Validation failed: Function '{FunctionName}' not found", functionName);
            return (false, $"Function '{functionName}' not found");
        }

        return (true, string.Empty);
    }


}
