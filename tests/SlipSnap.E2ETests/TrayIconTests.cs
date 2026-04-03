using FluentAssertions;
using SlipSnap.E2ETests.Helpers;

namespace SlipSnap.E2ETests;

public class TrayIconTests : IDisposable
{
    private readonly AppLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    [Fact(Skip = "Requires built and signed app — run manually")]
    public void TrayIcon_ShouldAppearOnLaunch()
    {
        _launcher.Start();
        Thread.Sleep(3000);

        // Verify the process is running (tray icon presence is hard to verify via UIA)
        _launcher.App.HasExited.Should().BeFalse("App should still be running with tray icon");
    }
}
