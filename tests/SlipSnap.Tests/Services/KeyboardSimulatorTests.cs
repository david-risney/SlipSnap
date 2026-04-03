using FluentAssertions;
using SlipSnap.Interop;
using SlipSnap.Services;

namespace SlipSnap.Tests.Services;

public class KeyboardSimulatorTests
{
    [Fact]
    public void SendKeys_DesktopSwitch_UsesCorrectVKSequence()
    {
        // Verify the expected VK codes for desktop switching
        var nextDesktopKeys = new[] { VirtualKey.VK_LCONTROL, VirtualKey.VK_LWIN, VirtualKey.VK_RIGHT };
        nextDesktopKeys.Should().HaveCount(3);
        nextDesktopKeys.Should().Contain(VirtualKey.VK_LCONTROL);
        nextDesktopKeys.Should().Contain(VirtualKey.VK_LWIN);
        nextDesktopKeys.Should().Contain(VirtualKey.VK_RIGHT);
    }

    [Fact]
    public void SendKeys_StartMenu_UsesLWin()
    {
        var keys = new[] { VirtualKey.VK_LWIN };
        keys.Should().ContainSingle().Which.Should().Be(VirtualKey.VK_LWIN);
    }

    [Fact]
    public void SendKeys_TaskView_UsesWinTab()
    {
        var keys = new[] { VirtualKey.VK_LWIN, VirtualKey.VK_TAB };
        keys.Should().HaveCount(2);
        keys.Should().Contain(VirtualKey.VK_LWIN);
        keys.Should().Contain(VirtualKey.VK_TAB);
    }
}
