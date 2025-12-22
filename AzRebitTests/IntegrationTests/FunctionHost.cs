using System.Diagnostics;

using AzRebit;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Infrastructure.StateStorage;

using Azure.Storage.Blobs;
using Azure.Storage.Queues;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.Azurite;

namespace AzRebitTests.IntegrationTests;

[CollectionDefinition("FunctionApp")]
public class FunctionAppCollection : ICollectionFixture<FunctionAppFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public class FunctionAppFixture : IAsyncLifetime
{
    private IContainer _functionContainer;
    private AzuriteContainer _azuriteContainer;
    private const int FunctionAppPort = 80;
    private const int AzuritePort = 10000;
    private const string FunctionAppImageName = "azrebit-function-app";
    private const string ContainerNetwork = "azrebit-test-network";

    public FunctionAppFixture()
    {
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
       
    }

    public HttpClient HttpClient { get; private set; }
    public string BaseUrl => $"http://localhost:7080";
    public ServiceProvider ServiceProvider { get; set; }
    public BlobContainerClient BlobResubmitContainer { get; private set; }
    public QueueClient FunctionOutputQueue { get; private set; }
    public string AzuriteConnectionString { get; private set; } = null!;
    public async Task InitializeAsync()
    {
        try
        {
            Console.WriteLine("Starting TestContainers setup...");
            // Create custom network for containers
            var network = new NetworkBuilder()
                .WithName(ContainerNetwork)
                .Build();
            await network.CreateAsync();

            //---build azurite storage ----
            _azuriteContainer = new AzuriteBuilder()
               .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
               .Build();
            await _azuriteContainer.StartAsync();
            Console.WriteLine($"Azurite container started on port {AzuritePort}");
            AzuriteConnectionString = _azuriteContainer.GetConnectionString();

            await StartFunctionAppContainer();
        }
        catch (Exception ex)
        {
            // Clean up any processes that might be running
            await DisposeAsync();

            // Re-throw with a clear message
            throw new InvalidOperationException(
                "CRITICAL: Function host failed to start. All tests will be skipped. " +
                $"Error: {ex.Message}", ex);
        }
    }
    private void CreateServiceCollection()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient("resubmit", c =>
        {
            c.BaseAddress = new Uri($"{BaseUrl}/api/azrebit/resubmit");
        });
        serviceCollection.AddHttpClient("workitems", c =>
        {
            c.BaseAddress = new Uri($"{BaseUrl}/api/azrebit/funcworkitems");
        });
        serviceCollection.AddSingleton<IResubmitStorage, BlobResubmitStorage>();
        serviceCollection.AddSingleton<IWorkItemStore, StorageTablePersistService>();
        serviceCollection.AddAzureClients(clients =>
        {
            clients.AddBlobServiceClient(AzuriteConnectionString).WithName("resubmitContainer");
            clients.AddQueueServiceClient(AzuriteConnectionString).WithName("queueClient");
            clients.AddTableServiceClient(AzuriteConnectionString).WithName(ResubmitFunctionWorkerExtension.InternalRebitStorageTable);
        });
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }
    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();

        if (_functionContainer != null)
        {
            await _functionContainer.DisposeAsync();
        }

        if (_azuriteContainer != null)
        {
            await _azuriteContainer.DisposeAsync();
        }
    }

    private async Task StartFunctionAppContainer()
    {
        Console.WriteLine("Starting function app container...");

        _functionContainer = new ContainerBuilder()
            .WithImage(FunctionAppImageName)
            .WithPortBinding(7080,true)
            .WithEnvironment("AzureWebJobsStorage", AzuriteConnectionString)
            .WithEnvironment("AZURE_FUNCTIONS_ENVIRONMENT", "Development")
            .WithNetwork(ContainerNetwork)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(
                        req => req.ForPort(FunctionAppPort).ForPath("/admin/host/status")))
            .Build();

        await _functionContainer.StartAsync();
        Console.WriteLine("Function app container started");
    }
}