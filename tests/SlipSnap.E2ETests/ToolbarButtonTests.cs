using System.Text.Json;
using FluentAssertions;
using SlipSnap.E2ETests.Helpers;

namespace SlipSnap.E2ETests;

[Collection("App")]
public class ToolbarButtonTests
{
    private readonly AppFixture _app;

    public ToolbarButtonTests(AppFixture app) => _app = app;

    [Fact]
    public void Toolbars_ShouldHaveExpectedButtons()
    {
        using var result = ScriptRunner.Run("inspect-toolbar.ps1");
        var toolbars = result.RootElement.GetProperty("toolbars");

        var allButtons = toolbars.EnumerateArray()
            .SelectMany(t => t.GetProperty("buttons").EnumerateArray())
            .Select(b => b.GetProperty("name").GetString()!)
            .ToList();

        // Default config has Task View + Prev Desktop on left, Start Menu + Next Desktop on right
        allButtons.Should().Contain("Task View");
        allButtons.Should().Contain("Previous Desktop");
        allButtons.Should().Contain("Start Menu");
        allButtons.Should().Contain("Next Desktop");
    }

    [Fact]
    public void AllButtons_ShouldBeEnabled()
    {
        using var result = ScriptRunner.Run("inspect-toolbar.ps1");
        var toolbars = result.RootElement.GetProperty("toolbars");

        foreach (var toolbar in toolbars.EnumerateArray())
        {
            foreach (var button in toolbar.GetProperty("buttons").EnumerateArray())
            {
                button.GetProperty("isEnabled").GetBoolean().Should().BeTrue(
                    $"button '{button.GetProperty("name").GetString()}' should be enabled");
            }
        }
    }

    [Fact]
    public void AllButtons_ShouldBeVisible()
    {
        using var result = ScriptRunner.Run("inspect-toolbar.ps1");
        var toolbars = result.RootElement.GetProperty("toolbars");

        foreach (var toolbar in toolbars.EnumerateArray())
        {
            foreach (var button in toolbar.GetProperty("buttons").EnumerateArray())
            {
                button.GetProperty("isOffscreen").GetBoolean().Should().BeFalse(
                    $"button '{button.GetProperty("name").GetString()}' should be on-screen");
            }
        }
    }
}
