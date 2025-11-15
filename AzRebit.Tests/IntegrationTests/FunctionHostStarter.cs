using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Tests.IntegrationTests;

public class FunctionHostStarter
{
    private static Process _hostProcess;
    private static HttpClient _httpClient;
    public static string BaseUrl { get; } = "http://localhost:7000";
    public static HttpClient GetHttpClient() => _httpClient;

    public static async Task StartFunctionHost()
    {
        // Start the Azure Functions host
        _hostProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "func",
                Arguments = "start --port 7000",
                WorkingDirectory = "",//autodiscover
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };


        _hostProcess.Start();

        // Initialize HttpClient
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromMinutes(1)
        };

        // Wait for the host to be ready
        await WaitForHostToBeReady(_httpClient, maxWaitTimeSeconds: 60);
    }
    private static async Task WaitForHostToBeReady(HttpClient client, int maxWaitTimeSeconds)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(maxWaitTimeSeconds);
        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                // Try to hit the admin endpoint or a health check endpoint
                // Azure Functions host typically exposes an admin API
                var response = await client.GetAsync("/admin/host/status");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Azure Functions host is ready!");
                 
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Host not ready yet, continue polling
            }
            catch (TaskCanceledException)
            {
                // Timeout on individual request, continue polling
            }

            await Task.Delay(1000); // Wait 1 second between polls
        }

        throw new TimeoutException($"Azure Functions host did not become ready within {maxWaitTimeSeconds} seconds");
    }

    public static void Dispose()
    {
        _httpClient?.Dispose();

        if (_hostProcess != null && !_hostProcess.HasExited)
        {
            _hostProcess.Kill(entireProcessTree: true);
            _hostProcess.WaitForExit(5000);
            _hostProcess.Dispose();
        }
    }

}
