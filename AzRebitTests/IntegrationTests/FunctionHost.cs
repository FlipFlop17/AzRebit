using System.Diagnostics;

using Azure.Storage.Blobs;
using Azure.Storage.Queues;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

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
    private Process _funcHostProcess;
    private readonly string _functionProjectPath;
    private readonly int _port;

    public HttpClient HttpClient { get; private set; }
    public string BaseUrl => $"http://localhost:{_port}";
    public ServiceProvider ServiceProvider { get; set; }
    public BlobContainerClient BlobResubmitContainer { get; private set; }
    public QueueClient FunctionOutputQueue { get; private set; }

    public FunctionAppFixture()
    {
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");
        var storageSetting = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        // Adjust this path to point to your function project directory
        var solutionDir = GetSolutionDirectory();
        _functionProjectPath = Path.Combine(solutionDir, "AzRebit.FunctionExample");
        _port = FindAvailablePort(); // Find an available port dynamically

        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient("resubmit", c =>
        {
            c.BaseAddress = new Uri($"{BaseUrl}/api/resubmit");
        });
        serviceCollection.AddAzureClients(clients =>
        {
            clients.AddBlobServiceClient(storageSetting).WithName("resubmitContainer");
            clients.AddQueueServiceClient(storageSetting).WithName("funcOutput");
        });
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Kill any orphaned func processes from previous runs
            await KillOrphanedFuncProcesses();

            // Ensure the function project is built
            await BuildFunctionProject();

            // Start the function host
            await StartFunctionHost();

            // Wait for the host to be ready
            await WaitForHostToBeReady();
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

    public async Task DisposeAsync()
    {
        Console.WriteLine("Disposing FunctionAppFixture - cleaning up resources...");

        try
        {
            HttpClient?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing HttpClient: {ex.Message}");
        }

        try
        {
            if (_funcHostProcess != null)
            {
                if (!_funcHostProcess.HasExited)
                {
                    Console.WriteLine($"Stopping function host process (PID: {_funcHostProcess.Id})...");
                    _funcHostProcess.Kill(entireProcessTree: true);

                    // Wait with timeout
                    var exitTask = _funcHostProcess.WaitForExitAsync();
                    if (await Task.WhenAny(exitTask, Task.Delay(5000)) == exitTask)
                    {
                        Console.WriteLine("Function host stopped successfully");
                    } else
                    {
                        Console.WriteLine("Function host did not stop within 5 seconds");
                    }
                } else
                {
                    Console.WriteLine($"Function host process already exited with code {_funcHostProcess.ExitCode}");
                }

                _funcHostProcess.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping function host: {ex.Message}");
        }

        // Extra safety: kill any remaining func.exe processes on our port
        await KillOrphanedFuncProcesses();

        Console.WriteLine("FunctionAppFixture disposed");
    }

    private async Task BuildFunctionProject()
    {
        Console.WriteLine($"Building function project at: {_functionProjectPath}");

        var buildProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build --configuration Debug",
                WorkingDirectory = _functionProjectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        buildProcess.Start();
        var output = await buildProcess.StandardOutput.ReadToEndAsync();
        var error = await buildProcess.StandardError.ReadToEndAsync();
        await buildProcess.WaitForExitAsync();

        if (buildProcess.ExitCode != 0)
        {
            Console.Error.WriteLine($"Build output: {output}");
            Console.Error.WriteLine($"Build error: {error}");
            throw new InvalidOperationException(
                $"Function build failed with exit code {buildProcess.ExitCode}. " +
                $"Error: {error}");
        }

        Console.WriteLine("Function project built successfully");
    }

    private async Task StartFunctionHost()
    {
        Console.WriteLine($"Starting function host on port {_port}...");

        _funcHostProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "func",
                Arguments = $"start --port {_port}",
                WorkingDirectory = _functionProjectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        // Capture output for debugging
        _funcHostProcess.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Console.WriteLine($"[FUNC HOST] {args.Data}");
            }
        };

        _funcHostProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Console.Error.WriteLine($"[FUNC HOST ERROR] {args.Data}");
            }
        };

        _funcHostProcess.Start();
        _funcHostProcess.BeginOutputReadLine();
        _funcHostProcess.BeginErrorReadLine();

        // Give it a moment to start
        await Task.Delay(2000);

        if (_funcHostProcess.HasExited)
        {
            throw new InvalidOperationException(
                $"Function host exited immediately with code {_funcHostProcess.ExitCode}. " +
                "Check the error output above for details.");
        }
    }

    private async Task WaitForHostToBeReady(int maxAttempts = 30)
    {
        Console.WriteLine("Waiting for function host to be ready...");

        for (int i = 0; i < maxAttempts; i++)
        {
            // Check if process has crashed
            if (_funcHostProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Function host crashed during startup with exit code {_funcHostProcess.ExitCode}");
            }

            try
            {
                var response = await HttpClient.GetAsync("/admin/host/status");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Function host is ready!");
                    return;
                }

                Console.WriteLine($"Attempt {i + 1}/{maxAttempts}: Host not ready yet (Status: {response.StatusCode})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attempt {i + 1}/{maxAttempts}: {ex.Message}");
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"Function host did not become ready after {maxAttempts} seconds. " +
            "Check the function host output above for errors.");
    }

    private static string GetSolutionDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }

        if (directory == null)
        {
            throw new InvalidOperationException("Could not find solution directory");
        }

        return directory.FullName;
    }

    private static int FindAvailablePort()
    {
        // Try default port first
        if (IsPortAvailable(7071))
        {
            return 7071;
        }

        // Try a range of ports
        for (int port = 7072; port <= 7100; port++)
        {
            if (IsPortAvailable(port))
            {
                Console.WriteLine($"Port 7071 is in use, using port {port} instead");
                return port;
            }
        }

        throw new InvalidOperationException("Could not find an available port in range 7071-7100");
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task KillOrphanedFuncProcesses()
    {
        try
        {
            // Kill any func.exe processes that might be lingering
            var funcProcesses = Process.GetProcessesByName("func");
            if (funcProcesses.Length > 0)
            {
                Console.WriteLine($"Found {funcProcesses.Length} orphaned func.exe process(es), killing them...");
                foreach (var process in funcProcesses)
                {
                    try
                    {
                        process.Kill(true);
                        await process.WaitForExitAsync();
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to kill process {process.Id}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error killing orphaned processes: {ex.Message}");
        }
    }
}