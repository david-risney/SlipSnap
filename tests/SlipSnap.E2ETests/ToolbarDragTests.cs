using FluentAssertions;
using SlipSnap.E2ETests.Helpers;

namespace SlipSnap.E2ETests;

public class ToolbarDragTests : IDisposable
{
    private readonly AppLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    [Fact(Skip = "Requires built and signed app — run manually")]
    public void Grip_ShouldExistOnToolbar()
    {
        _launcher.Start();
        Thread.Sleep(2000);

        var windows = _launcher.App.GetAllTopLevelWindows(_launcher.Automation);
        var toolbar = windows.First();

        var grip = toolbar.FindFirstDescendant(cf => cf.ByName("Grip"));
        grip.Should().NotBeNull("Toolbar should have a grip element for dragging");
    }
}
