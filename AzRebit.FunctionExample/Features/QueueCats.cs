using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzRebit.FunctionExample.Features;

internal class QueueCats
{
    private readonly ILogger<QueueCats> _logger;

    public QueueCats(ILogger<QueueCats> logger)
    {
        _logger = logger;
    }

    [Function("TransformCats")]
    [QueueOutput("transform-cats-output")]
    public string Run([QueueTrigger("transform-cats-queue")] string dogMessage)
    {
        _logger.LogInformation("queueMessage: "+dogMessage);
        return dogMessage + ";has been processed";
        
    }
}
