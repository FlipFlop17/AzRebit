using System.Diagnostics;

using AzRebit;
using AzRebit.Infrastructure.FileStorage;
using AzRebit.Infrastructure.StateStorage;

using Azure.Storage.Blobs;
using Azure.Storage.Queues;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

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
    private IContainer seqContainer;
    private AzuriteContainer _azuriteContainer;
    private const int FunctionAppPort = 80;
    private const int AzuritePort = 10000;
    private const string FunctionAppImageName = "azrebit-function-app";
    private const string ContainerNetwork = "azrebit-test-network";

    public FunctionAppFixture()
    {
        HostHttpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

    }

    public HttpClient HostHttpClient { get; private set; }
    public string BaseUrl => $"http://localhost:7080";
    public ServiceProvider ServiceProvider { get; set; }
    public BlobContainerClient BlobResubmitContainer { get; private set; }
    public QueueClient FunctionOutputQueue { get; private set; }
    public string AzuriteHostConnectionString { get; private set; } = null!;
    public string AzuriteAliasConnectionString { get; private set; } = null!;
    public string BlobSearchTag { get; set; }
    public async Task InitializeAsync()
    {
        try
        {
            Console.WriteLine("Starting TestContainers setup...");

            await PublishFunctionApp();
            // Create custom network for containers
            var network = new NetworkBuilder()
                .WithName(ContainerNetwork)
                .Build();
            await network.CreateAsync();

            //---build azurite storage ----
            //_azuriteContainer = new AzuriteBuilder()
            //   .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            //   .WithNetwork(ContainerNetwork)
            //   .WithNetworkAliases("azurite")
            //    .WithPortBinding(10000, 10000)  // Blob service
            //   .WithPortBinding(10001, 10001)  // Queue service  
            //   .WithPortBinding(10002, 10002)  // Table service
            //   .Build();

            //await _azuriteContainer.StartAsync();

            //Console.WriteLine($"Azurite container started on port {AzuritePort}");
            var localAzuriteConnectionString = "DefaultEndpointsProtocol=http;" +
                "AccountName=devstoreaccount1;" +
                "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
                "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;" +
                "QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;" +
                "TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";
            AzuriteHostConnectionString = localAzuriteConnectionString;
            AzuriteAliasConnectionString = AzuriteHostConnectionString.Replace("127.0.0.1", "host.docker.internal");
            //start seq server
            seqContainer = new ContainerBuilder()
                .WithName("seq")
                .WithNetwork(ContainerNetwork)
                .WithImage("datalust/seq:latest")
                .WithEnvironment("ACCEPT_EULA","Y")
                .WithEnvironment("SEQ_FIRSTRUN_NOAUTHENTICATION", "True")
                .WithPortBinding(5341,5341)
                .WithNetworkAliases("seq-container")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5341))
                .Build();
            await seqContainer.StartAsync();
            await StartFunctionAppContainer();

            CreateServiceCollection();
            BlobSearchTag = ServiceProvider.GetRequiredService<IResubmitStorage>().SearchTag;
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
    public HttpClient CreateResubmitClient()
        => ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("resubmit");
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
            clients.AddBlobServiceClient(AzuriteHostConnectionString).WithName(ResubmitFunctionWorkerExtension.BlobResubmitServiceClientName);
            clients.AddBlobServiceClient(AzuriteHostConnectionString).WithName("catsContainer");
            clients.AddQueueServiceClient(AzuriteHostConnectionString).WithName("queueClient");
            clients.AddTableServiceClient(AzuriteHostConnectionString).WithName(ResubmitFunctionWorkerExtension.InternalRebitStorageTable);
        });
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }
    public async Task DisposeAsync()
    {
        HostHttpClient?.Dispose();

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
        var functionCoreImage = "mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0";
        var publishFolder = Path.Combine(
            CommonDirectoryPath.GetSolutionDirectory().DirectoryPath,
            "AzRebit.FunctionExample/bin/Release/net8.0/publish");
        var azureFunctionCoreFolder = "/home/site/wwwroot";
        var logsDirectory = Path.Combine(
            CommonDirectoryPath.GetSolutionDirectory().DirectoryPath,
            "AzRebitTests/test-logs");
        _functionContainer = new ContainerBuilder()
            .WithName("func-integ-test")
            .WithImage(functionCoreImage)
            .WithBindMount(publishFolder, azureFunctionCoreFolder, DotNet.Testcontainers.Configurations.AccessMode.ReadWrite)
            .WithPortBinding(7080, 80)
            .WithEnvironment("AzureWebJobsStorage", AzuriteAliasConnectionString)
            .WithEnvironment("AZURE_FUNCTIONS_ENVIRONMENT", "Development")
            //.WithEnvironment("AzureWebJobsScriptRoot", "/home/site/wwwroot")
            //.WithEnvironment("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated")
            .WithEnvironment("AZREBIT_DELETE_RESUBMITION_FILE", "false")
            .WithEnvironment("AZURE_FUNCTION_TEST_BASE_URL", "http://host.docker.internal:7080")
            .WithEnvironment("SEQ_SERVER_URL", "http://host.docker.internal:5341")
            .WithNetwork(ContainerNetwork)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilInternalTcpPortIsAvailable(80))
            //.UntilHttpRequestIsSucceeded(
            //    req => req.ForPort(FunctionAppPort).ForPath("/admin/host/status")))
            //.WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(
            //    new FileStream(Path.Combine(logsDirectory,$"func-container-{DateTime.Now:yyyyMMdd-HHmmss}.log"),
            //    FileMode.Create,FileAccess.Write,FileShare.Read)))
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .Build();

        await _functionContainer.StartAsync();

        Console.WriteLine("Function app container started");
    }

    private async Task PublishFunctionApp()
    {
        Console.WriteLine("Publishing function app...");

        var projectPath = Path.Combine(
            CommonDirectoryPath.GetSolutionDirectory().DirectoryPath,
            "AzRebit.FunctionExample/AzRebit.FunctionExample.csproj");

        var publishFolder = Path.Combine(
            CommonDirectoryPath.GetSolutionDirectory().DirectoryPath,
            "AzRebit.FunctionExample/bin/Release/net8.0/publish");

        // Clean publish folder if it exists
        if (Directory.Exists(publishFolder))
        {
            Directory.Delete(publishFolder, true);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectPath}\" -c Release -o \"{publishFolder}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start dotnet publish process");
        }

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Publish failed: {error}");
        }

        Console.WriteLine("Function app published successfully");
    }
}