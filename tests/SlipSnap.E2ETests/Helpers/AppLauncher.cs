using System.Diagnostics;
using System.IO;
using FlaUI.Core;
using FlaUI.UIA3;

namespace SlipSnap.E2ETests.Helpers;

public class AppLauncher : IDisposable
{
    private Application? _app;
    private UIA3Automation? _automation;

    public Application App => _app ?? throw new InvalidOperationException("App not started");
    public UIA3Automation Automation => _automation ?? throw new InvalidOperationException("App not started");

    public void Start()
    {
        _automation = new UIA3Automation();

        // Find the built SlipSnap.exe
        string projectDir = FindProjectDir();
        string exePath = Path.Combine(projectDir, "src", "SlipSnap", "bin", "Debug",
            "net8.0-windows10.0.22621.0", "SlipSnap.exe");

        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"SlipSnap.exe not found at {exePath}. Build the project first.");
        }

        _app = Application.Launch(new ProcessStartInfo(exePath)
        {
            WorkingDirectory = Path.GetDirectoryName(exePath)
        });
    }

    public void Stop()
    {
        _app?.Close();
        _app?.Dispose();
        _app = null;
    }

    public void Dispose()
    {
        Stop();
        _automation?.Dispose();
        _automation = null;
    }

    private static string FindProjectDir()
    {
        // Walk up from test assembly to find the repo root (has src/ folder)
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "src")) &&
                Directory.Exists(Path.Combine(dir, "tests")))
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName!;
        }
        throw new DirectoryNotFoundException("Could not find repository root with src/ and tests/ folders");
    }
}
