using FluentAssertions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using SlipSnap.E2ETests.Helpers;

namespace SlipSnap.E2ETests;

public class ToolbarButtonTests : IDisposable
{
    private readonly AppLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    [Fact(Skip = "Requires built and signed app — run manually")]
    public void StartMenu_Button_ShouldExist()
    {
        _launcher.Start();
        Thread.Sleep(2000);

        var windows = _launcher.App.GetAllTopLevelWindows(_launcher.Automation);
        var toolbar = windows.First();

        var startBtn = toolbar.FindFirstDescendant(cf =>
            cf.ByAutomationId("BtnStartMenu").Or(cf.ByName("Start Menu")));
        startBtn.Should().NotBeNull("Start Menu button should exist");
    }

    [Fact(Skip = "Requires built and signed app — run manually")]
    public void TaskView_Button_ShouldExist()
    {
        _launcher.Start();
        Thread.Sleep(2000);

        var windows = _launcher.App.GetAllTopLevelWindows(_launcher.Automation);
        var toolbar = windows.First();

        var taskViewBtn = toolbar.FindFirstDescendant(cf =>
            cf.ByAutomationId("BtnTaskView").Or(cf.ByName("Task View")));
        taskViewBtn.Should().NotBeNull("Task View button should exist");
    }
}
