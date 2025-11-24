using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.FunctionExample.Features;

internal class TransformCats_QueueTrigger
{
    private readonly ILogger<TransformCats_QueueTrigger> _logger;

    public TransformCats_QueueTrigger(ILogger<TransformCats_QueueTrigger> logger)
    {
        _logger = logger;
    }

    [Function("TransformCats")]
    [QueueOutput("transform-cats-output")]
    public string Run([QueueTrigger("transform-cats-queue", Connection = "AzureWebJobsStorage")] string dogMessage)
    {
        _logger.LogInformation("queueMessage: "+dogMessage);
        return dogMessage + ";has been processed";
        
    }
}
