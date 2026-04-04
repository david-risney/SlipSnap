using FluentAssertions;
using SlipSnap.E2ETests.Helpers;

namespace SlipSnap.E2ETests;

[Collection("App")]
public class ToolbarVisibilityTests
{
    private readonly AppFixture _app;

    public ToolbarVisibilityTests(AppFixture app) => _app = app;

    [Fact]
    public void Toolbar_ShouldBeVisibleOnLaunch()
    {
        using var result = ScriptRunner.Run("find-windows.ps1");
        var root = result.RootElement;

        root.GetProperty("windowCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        var windows = root.GetProperty("windows");
        windows.EnumerateArray().Should().Contain(w =>
            w.GetProperty("name").GetString()!.Contains("SlipSnap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Toolbar_ShouldHaveDesktopNavigationButtons()
    {
        using var result = ScriptRunner.Run("inspect-toolbar.ps1");
        var toolbars = result.RootElement.GetProperty("toolbars");

        foreach (var toolbar in toolbars.EnumerateArray())
        {
            toolbar.GetProperty("buttonCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        }
    }

    [Fact]
    public void Toolbar_WindowsShouldNotBeOffscreen()
    {
        using var result = ScriptRunner.Run("find-windows.ps1");
        var windows = result.RootElement.GetProperty("windows");

        foreach (var w in windows.EnumerateArray())
        {
            w.GetProperty("isOffscreen").GetBoolean().Should().BeFalse();
        }
    }
}
