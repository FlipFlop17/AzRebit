using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Tests.IntegrationTests;

//[TestClass]
public class FunctionHostStarter
{
    private static Process _hostProcess;
    private static HttpClient _httpClient;
    private static readonly object _lockObject = new object();
    private static bool _isStarting = false;
    
    public static string BaseUrl { get; } = "http://localhost:7000";
    public static HttpClient GetHttpClient() => _httpClient;
    
    
    //[AssemblyInitialize]
    public static void StartFunctionHost(TestContext context)
    {
        lock (_lockObject)
        {
            if (_isStarting)
                throw new InvalidOperationException("Function host is already starting");
            _isStarting = true;
        }

        try
        {
            // Clean up any existing process first
            Dispose(context);

            // Start the Azure Functions host
            _hostProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "func",
                    Arguments = "start --port 7000",
                    WorkingDirectory = "", // autodiscover
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            // Monitor process exit to detect early failures
            var processExitedTcs = new TaskCompletionSource<bool>();
            _hostProcess.Exited += (sender, e) =>
            {
                processExitedTcs.TrySetResult(true);
            };

            // Capture output for debugging
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            _hostProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    Debug.WriteLine($"FUNC OUTPUT: {e.Data}");
                }
            };

            _hostProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    Debug.WriteLine($"FUNC ERROR: {e.Data}");
                }
            };

            if (!_hostProcess.Start())
            {
                throw new InvalidOperationException("Failed to start Azure Functions host process");
            }

            _hostProcess.BeginOutputReadLine();
            _hostProcess.BeginErrorReadLine();

            // Initialize HttpClient
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30) // Shorter timeout for individual requests
            };

            // Wait for the host to be ready with proper error handling
            WaitForHostToBeReady(_httpClient, processExitedTcs.Task, outputBuilder, errorBuilder, maxWaitTimeSeconds: 60)
                .GetAwaiter().GetResult();
        }
        catch
        {
            // Clean up on failure
            Dispose(context);
            throw;
        }
        finally
        {
            lock (_lockObject)
            {
                _isStarting = false;
            }
        }
    }

    private static async Task WaitForHostToBeReady(
        HttpClient client, 
        Task processExitedTask, 
        StringBuilder outputBuilder, 
        StringBuilder errorBuilder,
        int maxWaitTimeSeconds)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(maxWaitTimeSeconds);
        var delay = TimeSpan.FromSeconds(1);

        while (DateTime.UtcNow - startTime < timeout)
        {
            // Check if process has exited unexpectedly
            if (processExitedTask.IsCompleted)
            {
                var exitCode = _hostProcess?.ExitCode ?? -1;
                var output = outputBuilder.ToString();
                var errors = errorBuilder.ToString();
                
                throw new InvalidOperationException(
                    $"Azure Functions host process exited unexpectedly with code {exitCode}.\n" +
                    $"Output: {output}\n" +
                    $"Errors: {errors}");
            }

            try
            {
                // Try to hit the admin endpoint or a health check endpoint
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await client.GetAsync("/admin/host/status", cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("Azure Functions host is ready");
                    return;
                }
                
                Debug.WriteLine($"Host status check returned: {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Host not ready yet (HttpRequestException): {ex.Message}");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Debug.WriteLine("Host status check timed out, continuing to poll");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Host status check was cancelled, continuing to poll");
            }

            // Wait before next poll, but also check if process exited during delay
            var delayTask = Task.Delay(delay);
            var completedTask = await Task.WhenAny(delayTask, processExitedTask);
            
            if (completedTask == processExitedTask)
            {
                continue; // Will be caught in next iteration
            }
        }

        // Final check if process is still running
        if (_hostProcess?.HasExited == true)
        {
            var exitCode = _hostProcess.ExitCode;
            var output = outputBuilder.ToString();
            var errors = errorBuilder.ToString();
            
            throw new InvalidOperationException(
                $"Azure Functions host process exited during startup with code {exitCode}.\n" +
                $"Output: {output}\n" +
                $"Errors: {errors}");
        }

        throw new TimeoutException(
            $"Azure Functions host did not become ready within {maxWaitTimeSeconds} seconds.\n" +
            $"Output: {outputBuilder}\n" +
            $"Errors: {errorBuilder}");
    }


    //[AssemblyCleanup]
    public static void Dispose(TestContext context)
    {
        try
        {
            _httpClient?.Dispose();
            _httpClient = null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error disposing HttpClient: {ex.Message}");
        }

        try
        {
            if (_hostProcess != null && !_hostProcess.HasExited)
            {
                Debug.WriteLine("Terminating Azure Functions host process...");
                
                // Try graceful shutdown first
                _hostProcess.CloseMainWindow();
                
                // Wait a bit for graceful shutdown
                if (!_hostProcess.WaitForExit(5000))
                {
                    Debug.WriteLine("Graceful shutdown failed, killing process tree...");
                    _hostProcess.Kill(entireProcessTree: true);
                    _hostProcess.WaitForExit(5000);
                }
                
                Debug.WriteLine($"Process exited with code: {_hostProcess.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error terminating process: {ex.Message}");
        }
        finally
        {
            try
            {
                _hostProcess?.Dispose();
                _hostProcess = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing process: {ex.Message}");
            }
        }
    }
}
