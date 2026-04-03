using FluentAssertions;
using SlipSnap.Models;

namespace SlipSnap.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void CreateDefault_ReturnsValidSettings()
    {
        var settings = AppSettings.CreateDefault();

        settings.Theme.Should().Be(ThemeMode.Auto);
        settings.OpacityPercent.Should().Be(80);
        settings.FullscreenOnly.Should().BeFalse();
        settings.HideInRdpSession.Should().BeTrue();
        settings.Toolbars.Should().HaveCount(4);
        settings.Toolbars[ToolbarEdge.Left].IsEnabled.Should().BeTrue();
        settings.Toolbars[ToolbarEdge.Right].IsEnabled.Should().BeFalse();
        settings.Toolbars[ToolbarEdge.Top].IsEnabled.Should().BeFalse();
        settings.Toolbars[ToolbarEdge.Bottom].IsEnabled.Should().BeFalse();
    }

    [Theory]
    [InlineData(5, 10)]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(10, 10)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(200, 100)]
    public void Normalize_ClampsOpacityPercent(int input, int expected)
    {
        var settings = new AppSettings { OpacityPercent = input };
        settings.Normalize();
        settings.OpacityPercent.Should().Be(expected);
    }

    [Fact]
    public void Normalize_PopulatesMissingEdges()
    {
        var settings = new AppSettings
        {
            Toolbars = new Dictionary<ToolbarEdge, ToolbarConfig>
            {
                [ToolbarEdge.Left] = ToolbarConfig.CreateDefault(ToolbarEdge.Left, true)
            }
        };

        settings.Normalize();

        settings.Toolbars.Should().HaveCount(4);
        settings.Toolbars.Should().ContainKey(ToolbarEdge.Right);
        settings.Toolbars.Should().ContainKey(ToolbarEdge.Top);
        settings.Toolbars.Should().ContainKey(ToolbarEdge.Bottom);
    }

    [Fact]
    public void Normalize_FixesEdgeMismatch()
    {
        var settings = new AppSettings();
        settings.Toolbars[ToolbarEdge.Left].Edge = ToolbarEdge.Right; // mismatch

        settings.Normalize();

        settings.Toolbars[ToolbarEdge.Left].Edge.Should().Be(ToolbarEdge.Left);
    }
}

public class ToolbarConfigTests
{
    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1.0, 1.0)]
    [InlineData(1.5, 1.0)]
    public void Normalize_ClampsPositionPercent(double input, double expected)
    {
        var config = new ToolbarConfig { PositionPercent = input };
        config.Normalize();
        config.PositionPercent.Should().Be(expected);
    }

    [Fact]
    public void Normalize_RemovesDuplicateButtons()
    {
        var config = new ToolbarConfig
        {
            Buttons = [ToolbarButtonType.StartMenu, ToolbarButtonType.StartMenu, ToolbarButtonType.TaskView]
        };

        config.Normalize();

        config.Buttons.Should().HaveCount(2);
        config.Buttons.Should().ContainInOrder(ToolbarButtonType.StartMenu, ToolbarButtonType.TaskView);
    }

    [Fact]
    public void Normalize_AddsDefaultButtonsWhenEnabledWithEmpty()
    {
        var config = new ToolbarConfig
        {
            IsEnabled = true,
            Buttons = []
        };

        config.Normalize();

        config.Buttons.Should().HaveCount(4);
    }
}
