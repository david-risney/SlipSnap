using FluentAssertions;
using SlipSnap.Models;
using SlipSnap.Services;
using SlipSnap.ViewModels;

namespace SlipSnap.Tests.ViewModels;

public class SettingsViewModelTests
{
    [Fact]
    public void Constructor_LoadsSettingsFromService()
    {
        var service = new FakeSettingsService();
        var vm = new SettingsViewModel(service);

        vm.Theme.Should().Be(ThemeMode.Auto);
        vm.OpacityPercent.Should().Be(80);
    }

    [Fact]
    public void OpacityPercent_ClampedTo10_100()
    {
        var service = new FakeSettingsService();
        var vm = new SettingsViewModel(service);

        vm.OpacityPercent = 5;
        vm.OpacityPercent.Should().Be(10);

        vm.OpacityPercent = 150;
        vm.OpacityPercent.Should().Be(100);
    }

    [Fact]
    public void SetTheme_SavesSettings()
    {
        var service = new FakeSettingsService();
        var vm = new SettingsViewModel(service);

        vm.Theme = ThemeMode.Dark;

        service.LastSaved.Should().NotBeNull();
        service.LastSaved!.Theme.Should().Be(ThemeMode.Dark);
    }

    [Fact]
    public void SetOpacity_SavesSettings()
    {
        var service = new FakeSettingsService();
        var vm = new SettingsViewModel(service);

        vm.OpacityPercent = 50;

        service.LastSaved.Should().NotBeNull();
        service.LastSaved!.OpacityPercent.Should().Be(50);
    }

    [Fact]
    public void ToggleEdge_SavesSettings()
    {
        var service = new FakeSettingsService();
        var vm = new SettingsViewModel(service);

        vm.IsRightEnabled = true;

        service.LastSaved.Should().NotBeNull();
        service.LastSaved!.Toolbars[ToolbarEdge.Right].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void FullscreenOnly_SavesSettings()
    {
        var service = new FakeSettingsService();
        var vm = new SettingsViewModel(service);

        vm.FullscreenOnly = true;

        service.LastSaved.Should().NotBeNull();
        service.LastSaved!.FullscreenOnly.Should().BeTrue();
    }

    [Fact]
    public void HideInRdpSession_SavesSettings()
    {
        var service = new FakeSettingsService();
        var vm = new SettingsViewModel(service);

        vm.HideInRdpSession = false;

        service.LastSaved.Should().NotBeNull();
        service.LastSaved!.HideInRdpSession.Should().BeFalse();
    }

    private class FakeSettingsService : ISettingsService
    {
        public AppSettings? LastSaved { get; private set; }

        public AppSettings Load() => AppSettings.CreateDefault();

        public void Save(AppSettings settings) => LastSaved = settings;

        public event EventHandler<AppSettings>? SettingsChanged;

        public void RaiseChanged(AppSettings s) => SettingsChanged?.Invoke(this, s);
    }
}
