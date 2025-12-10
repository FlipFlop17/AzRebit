using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace AzRebit.IntegrationTests;

[CollectionDefinition("FunctionApp")]
public class FunctionCollection : ICollectionFixture<FunctionAppFixture> { }

public class FunctionAppFixture : IAsyncLifetime
{
    private Process _funcHostProcess;
    private readonly string _functionProjectPath;
    private readonly int _port;

    public HttpClient HttpClient { get; private set; }
    public string BaseUrl => $"http://localhost:{_port}";

    public FunctionAppFixture()
    {
        // Adjust this path to point to your function project directory
        var solutionDir = GetSolutionDirectory();
        _functionProjectPath = Path.Combine(solutionDir, "AzRebit.FunctionExample");
        _port = 7071; // Default Azure Functions port

        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task InitializeAsync()
    {
        // Ensure the function project is built
        await BuildFunctionProject();

        // Start the function host
        await StartFunctionHost();

        // Wait for the host to be ready
        await WaitForHostToBeReady();
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();

        if (_funcHostProcess != null && !_funcHostProcess.HasExited)
        {
            _funcHostProcess.Kill(entireProcessTree: true);
            await _funcHostProcess.WaitForExitAsync();
            _funcHostProcess.Dispose();
        }
    }

    private async Task BuildFunctionProject()
    {
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
        await buildProcess.WaitForExitAsync();

        if (buildProcess.ExitCode != 0)
        {
            var error = await buildProcess.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Function build failed: {error}");
        }
    }

    private async Task StartFunctionHost()
    {
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
                $"Function host exited immediately with code {_funcHostProcess.ExitCode}");
        }
    }

    private async Task WaitForHostToBeReady(int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var response = await HttpClient.GetAsync("/admin/host/status");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Function host is ready!");
                    return;
                }
            }
            catch
            {
                // Host not ready yet
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException("Function host did not become ready in time");
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
}
