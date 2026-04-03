using FluentAssertions;
using FlaUI.Core.AutomationElements;
using SlipSnap.E2ETests.Helpers;

namespace SlipSnap.E2ETests;

public class ToolbarVisibilityTests : IDisposable
{
    private readonly AppLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    [Fact(Skip = "Requires built and signed app — run manually")]
    public void Toolbar_ShouldBeVisibleOnLaunch()
    {
        _launcher.Start();
        Thread.Sleep(2000); // Wait for app startup

        var mainWindow = _launcher.App.GetAllTopLevelWindows(_launcher.Automation);
        mainWindow.Should().NotBeEmpty("SlipSnap should have at least one window");

        // Find toolbar window by automation properties
        var toolbar = mainWindow.FirstOrDefault(w =>
            w.Name?.Contains("SlipSnap", StringComparison.OrdinalIgnoreCase) == true);
        toolbar.Should().NotBeNull("Should find a SlipSnap toolbar window");
    }

    [Fact(Skip = "Requires built and signed app — run manually")]
    public void Toolbar_ShouldHaveDesktopNavigationButtons()
    {
        _launcher.Start();
        Thread.Sleep(2000);

        var windows = _launcher.App.GetAllTopLevelWindows(_launcher.Automation);
        var toolbar = windows.First();

        var buttons = toolbar.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button));
        buttons.Should().HaveCountGreaterThanOrEqualTo(2, "Toolbar should have at least Next/Prev Desktop buttons");
    }
}
