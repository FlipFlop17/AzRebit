using AzRebit;
using AzRebit.FunctionExample.Infra;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddHttpClient();

builder.Services.AddAzureClients(clients =>
{
    clients.AddQueueServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"))
    .WithName("function-output-queue").ConfigureOptions(c =>
    {
        c.MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64;
    });
});

builder.Services.AddSingleton<IFunctionOutput, QueueStorage>();
builder.AddResubmitEndpoint();

if(builder.Environment.IsDevelopment())
{
    builder.Logging.AddSeq();
};
builder.Logging.AddFilter("Azure.Core", LogLevel.Error);
builder.Logging.AddFilter("Azure.Storage", LogLevel.Error);
builder.Logging.AddFilter("Host.General", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

builder.Build().Run();
