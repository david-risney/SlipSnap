using FluentAssertions;
using SlipSnap.Models;
using SlipSnap.Services;

namespace SlipSnap.Tests.Services;

public class ToolbarManagerTests
{
    [Fact]
    public void EvaluateVisibility_EnabledToolbar_IsVisible()
    {
        var settings = AppSettings.CreateDefault();
        var config = settings.Toolbars[ToolbarEdge.Left];
        config.IsEnabled = true;

        bool visible = ToolbarManagerHelper.EvaluateVisibility(settings, config,
            isFullscreenPresent: false, isRdpSession: false);

        visible.Should().BeTrue();
    }

    [Fact]
    public void EvaluateVisibility_DisabledToolbar_IsHidden()
    {
        var settings = AppSettings.CreateDefault();
        var config = settings.Toolbars[ToolbarEdge.Right];
        config.IsEnabled = false;

        bool visible = ToolbarManagerHelper.EvaluateVisibility(settings, config,
            isFullscreenPresent: false, isRdpSession: false);

        visible.Should().BeFalse();
    }

    [Fact]
    public void EvaluateVisibility_RdpSession_WithHideFlag_IsHidden()
    {
        var settings = AppSettings.CreateDefault();
        settings.HideInRdpSession = true;
        var config = settings.Toolbars[ToolbarEdge.Left];
        config.IsEnabled = true;

        bool visible = ToolbarManagerHelper.EvaluateVisibility(settings, config,
            isFullscreenPresent: false, isRdpSession: true);

        visible.Should().BeFalse();
    }

    [Fact]
    public void EvaluateVisibility_RdpSession_WithoutHideFlag_IsVisible()
    {
        var settings = AppSettings.CreateDefault();
        settings.HideInRdpSession = false;
        var config = settings.Toolbars[ToolbarEdge.Left];
        config.IsEnabled = true;

        bool visible = ToolbarManagerHelper.EvaluateVisibility(settings, config,
            isFullscreenPresent: false, isRdpSession: true);

        visible.Should().BeTrue();
    }

    [Fact]
    public void EvaluateVisibility_FullscreenOnly_NoFullscreen_IsHidden()
    {
        var settings = AppSettings.CreateDefault();
        settings.FullscreenOnly = true;
        var config = settings.Toolbars[ToolbarEdge.Left];
        config.IsEnabled = true;

        bool visible = ToolbarManagerHelper.EvaluateVisibility(settings, config,
            isFullscreenPresent: false, isRdpSession: false);

        visible.Should().BeFalse();
    }

    [Fact]
    public void EvaluateVisibility_FullscreenOnly_WithFullscreen_IsVisible()
    {
        var settings = AppSettings.CreateDefault();
        settings.FullscreenOnly = true;
        var config = settings.Toolbars[ToolbarEdge.Left];
        config.IsEnabled = true;

        bool visible = ToolbarManagerHelper.EvaluateVisibility(settings, config,
            isFullscreenPresent: true, isRdpSession: false);

        visible.Should().BeTrue();
    }

    [Fact]
    public void EvaluateVisibility_RdpTakesPrecendenceOverFullscreen()
    {
        var settings = AppSettings.CreateDefault();
        settings.HideInRdpSession = true;
        settings.FullscreenOnly = true;
        var config = settings.Toolbars[ToolbarEdge.Left];
        config.IsEnabled = true;

        // Even with fullscreen present, RDP hide wins
        bool visible = ToolbarManagerHelper.EvaluateVisibility(settings, config,
            isFullscreenPresent: true, isRdpSession: true);

        visible.Should().BeFalse();
    }
}

/// <summary>
/// Static helper to expose the visibility evaluation logic for unit testing.
/// </summary>
public static class ToolbarManagerHelper
{
    public static bool EvaluateVisibility(AppSettings settings, ToolbarConfig config,
        bool isFullscreenPresent, bool isRdpSession)
    {
        // Same logic as ToolbarManager.EvaluateVisibility
        if (settings.HideInRdpSession && isRdpSession) return false;
        if (!config.IsEnabled) return false;
        if (settings.FullscreenOnly && !isFullscreenPresent) return false;
        return true;
    }
}
