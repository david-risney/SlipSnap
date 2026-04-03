using FluentAssertions;
using SlipSnap.Interop;
using SlipSnap.Services;
using SlipSnap.ViewModels;

namespace SlipSnap.Tests.ViewModels;

public class ToolbarViewModelTests
{
    [Fact]
    public void NextDesktopCommand_InvokesKeyboardSimulator()
    {
        var simulator = new RecordingKeyboardSimulator();
        var vm = new ToolbarViewModel(simulator);

        vm.NextDesktopCommand.Execute(null);

        simulator.Invocations.Should().ContainSingle();
        simulator.Invocations[0].Should().Contain(VirtualKey.VK_RIGHT);
        simulator.Invocations[0].Should().Contain(VirtualKey.VK_LCONTROL);
        simulator.Invocations[0].Should().Contain(VirtualKey.VK_LWIN);
    }

    [Fact]
    public void PrevDesktopCommand_InvokesKeyboardSimulator()
    {
        var simulator = new RecordingKeyboardSimulator();
        var vm = new ToolbarViewModel(simulator);

        vm.PrevDesktopCommand.Execute(null);

        simulator.Invocations.Should().ContainSingle();
        simulator.Invocations[0].Should().Contain(VirtualKey.VK_LEFT);
        simulator.Invocations[0].Should().Contain(VirtualKey.VK_LCONTROL);
        simulator.Invocations[0].Should().Contain(VirtualKey.VK_LWIN);
    }

    [Fact]
    public void StartMenuCommand_InvokesKeyboardSimulator()
    {
        var simulator = new RecordingKeyboardSimulator();
        var vm = new ToolbarViewModel(simulator);

        vm.StartMenuCommand.Execute(null);

        simulator.Invocations.Should().ContainSingle();
        simulator.Invocations[0].Should().Contain(VirtualKey.VK_LWIN);
    }

    [Fact]
    public void TaskViewCommand_InvokesKeyboardSimulator()
    {
        var simulator = new RecordingKeyboardSimulator();
        var vm = new ToolbarViewModel(simulator);

        vm.TaskViewCommand.Execute(null);

        simulator.Invocations.Should().ContainSingle();
        simulator.Invocations[0].Should().Contain(VirtualKey.VK_LWIN);
        simulator.Invocations[0].Should().Contain(VirtualKey.VK_TAB);
    }

    private class RecordingKeyboardSimulator : IKeyboardSimulator
    {
        public List<VirtualKey[]> Invocations { get; } = [];

        public void SendKeys(params VirtualKey[] keys)
        {
            Invocations.Add(keys);
        }
    }
}
