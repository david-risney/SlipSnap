using System.Diagnostics;
using FluentAssertions;
using SlipSnap.E2ETests.Helpers;

namespace SlipSnap.E2ETests;

[Collection("App")]
public class TrayIconTests
{
    private readonly AppFixture _app;

    public TrayIconTests(AppFixture app) => _app = app;

    [Fact]
    public void App_ShouldStayRunningWithTrayIcon()
    {
        var proc = Process.GetProcessById(_app.ProcessId);
        proc.HasExited.Should().BeFalse("App should still be running with tray icon");
    }
}
