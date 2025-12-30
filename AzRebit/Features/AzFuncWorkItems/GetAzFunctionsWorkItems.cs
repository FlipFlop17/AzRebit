using AzRebit.Domain.Exceptions;
using AzRebit.Domain.Results;
using AzRebit.Infrastructure.StateStorage;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzRebit.Features.AzFuncWorkItems
{
    internal class GetAzFunctionsWorkItems
    {
        private readonly ILogger<GetAzFunctionsWorkItems> _logger;
        private readonly IWorkItemStore _resubmitStateStore;

        public GetAzFunctionsWorkItems(ILogger<GetAzFunctionsWorkItems> logger, IWorkItemStore resubmitStateStore)
        {
            _logger = logger;
            _resubmitStateStore = resubmitStateStore;
        }

        [Function("WorkItemInfo")]
        public async Task<HttpResponseData> GetResubmitStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, ["get"], Route = "azrebit/funcworkitems/{invocationId?}")] HttpRequestData req,
            string? invocationId, FunctionContext executionContext)
        {
            var response = req.CreateResponse();
            string? continuationToken = default;
            if (req.Url.Query.Length > 0)
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                continuationToken = query["continuationToken"];
            }

            var handlerResponse = await Handle(invocationId, continuationToken);
            if (handlerResponse.Data?.Count <= 0)
            {
                response.StatusCode = System.Net.HttpStatusCode.NotFound;
            }
            await response.WriteAsJsonAsync(handlerResponse);
            return response;
        }


        public async Task<RebitActionResult<List<AzFuncWorkItemInfoDto>>> Handle(string? invocationId, string? continuationToken)
        {
            List<WorkItemEntity> resubmitData = new();

            try
            {
                if (string.IsNullOrEmpty(invocationId))
                {
                    var allData = await _resubmitStateStore.GetAsync(continuationToken);
                    resubmitData.AddRange(allData.Items);
                } else
                {
                    var dataById = await _resubmitStateStore.GetEntityByInvocationId(invocationId);
                    if (dataById is not null)
                        resubmitData.Add(dataById);
                }

                var workItems = resubmitData
                    .Select(i => MapToResponseDto(i))
                    .ToList();
                var msg = workItems.Count <= 0 ? AzRebitErrorType.NotFound.ToString() : string.Empty;
                return RebitActionResult<List<AzFuncWorkItemInfoDto>>.Success(workItems, msg);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "Unexpected error in fetching all work items");
                return RebitActionResult<List<AzFuncWorkItemInfoDto>>.Failure(e.Message);
            }

        }

        private AzFuncWorkItemInfoDto MapToResponseDto(WorkItemEntity processedItem)
        {
            return new AzFuncWorkItemInfoDto(processedItem.PartitionKey,
                processedItem.RowKey,
                processedItem.FilePath,
                processedItem.ResubmitCount
            );
        }
    }
}
