using System.IO;
using System.Text.Json;
using FluentAssertions;
using SlipSnap.E2ETests.Helpers;

namespace SlipSnap.E2ETests;

[Collection("App")]
public class ScreenshotTests
{
    private readonly AppFixture _app;

    public ScreenshotTests(AppFixture app) => _app = app;

    [Fact]
    public void Toolbar_Screenshot_ShouldCapture()
    {
        // Get a toolbar window handle
        using var windows = ScriptRunner.Run("find-windows.ps1");
        var windowArray = windows.RootElement.GetProperty("windows").EnumerateArray().ToList();
        windowArray.Should().NotBeEmpty("app should have at least one toolbar window");

        var handle = windowArray[0].GetProperty("nativeHandle").GetInt32();

        using var result = ScriptRunner.Run("take-screenshot.ps1", $"-WindowHandle {handle}");
        var root = result.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("width").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("height").GetInt32().Should().BeGreaterThan(0);

        var path = root.GetProperty("path").GetString()!;
        File.Exists(path).Should().BeTrue("screenshot file should exist on disk");

        // Cleanup
        File.Delete(path);
    }
}
