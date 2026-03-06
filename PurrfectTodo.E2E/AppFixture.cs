using System.Diagnostics;
using System.Net;

namespace PurrfectTodo.E2E;

/// <summary>
/// NUnit SetUpFixture that ensures the PurrfectTodo app is running on
/// http://localhost:5059 before any tests execute, and tears it down
/// afterwards if this fixture started it.
/// </summary>
[Category("E2E")]
[SetUpFixture]
public class AppFixture
{
    private static readonly string BaseUrl = "http://localhost:5059";
    private static readonly string RunDevCmd = FindRunDevCmd();

    private static string FindRunDevCmd()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "run-dev.cmd");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("run-dev.cmd not found walking up from " + AppContext.BaseDirectory);
    }
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private Process? _appProcess;

    [OneTimeSetUp]
    public async Task StartApp()
    {
        if (await IsAppRunningAsync())
        {
            Console.WriteLine($"[AppFixture] App already running at {BaseUrl}. Skipping startup.");
            return;
        }

        Console.WriteLine($"[AppFixture] App not detected — launching run-dev.cmd …");

        _appProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{RunDevCmd}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        _appProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) Console.WriteLine($"[app] {e.Data}");
        };
        _appProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) Console.WriteLine($"[app-err] {e.Data}");
        };

        _appProcess.Start();
        _appProcess.BeginOutputReadLine();
        _appProcess.BeginErrorReadLine();

        Console.WriteLine($"[AppFixture] Waiting for app to become ready on {BaseUrl} …");
        var deadline = DateTime.UtcNow + StartupTimeout;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollInterval);
            if (await IsAppRunningAsync())
            {
                Console.WriteLine($"[AppFixture] App is ready.");
                return;
            }
        }

        throw new TimeoutException(
            $"PurrfectTodo did not become ready on {BaseUrl} within {StartupTimeout.TotalSeconds}s.");
    }

    [OneTimeTearDown]
    public void StopApp()
    {
        if (_appProcess is null) return;

        Console.WriteLine("[AppFixture] Stopping app process …");
        try
        {
            if (!_appProcess.HasExited)
            {
                _appProcess.Kill(entireProcessTree: true);
                _appProcess.WaitForExit(5_000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppFixture] Error stopping process: {ex.Message}");
        }
        finally
        {
            _appProcess.Dispose();
            _appProcess = null;
        }
    }

    private static async Task<bool> IsAppRunningAsync()
    {
        try
        {
            using var client = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
            client.Timeout = TimeSpan.FromSeconds(3);

            var response = await client.GetAsync(BaseUrl);

            // 200 OK or 3xx redirect both indicate the server is up
            return response.StatusCode == HttpStatusCode.OK
                || ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400);
        }
        catch
        {
            return false;
        }
    }
}
