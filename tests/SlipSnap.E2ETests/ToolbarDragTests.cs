using System.Text.Json;
using FluentAssertions;
using SlipSnap.E2ETests.Helpers;

namespace SlipSnap.E2ETests;

[Collection("App")]
public class ToolbarDragTests
{
    private readonly AppFixture _app;

    public ToolbarDragTests(AppFixture app) => _app = app;

    [Fact]
    public void Toolbars_ShouldBeDockedToCorrectEdges()
    {
        using var result = ScriptRunner.Run("inspect-toolbar.ps1");
        var toolbars = result.RootElement.GetProperty("toolbars");

        var edges = toolbars.EnumerateArray()
            .Select(t => t.GetProperty("edge").GetString()!)
            .ToList();

        edges.Should().Contain("Left");
        edges.Should().Contain("Right");
    }

    [Fact]
    public void LeftToolbar_ShouldBeAtLeftEdge()
    {
        using var result = ScriptRunner.Run("inspect-toolbar.ps1");
        var toolbars = result.RootElement.GetProperty("toolbars");

        var left = toolbars.EnumerateArray()
            .First(t => t.GetProperty("edge").GetString() == "Left");

        left.GetProperty("bounds").GetProperty("x").GetInt32().Should().Be(0);
    }
}
