using System.IO;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SlipSnap.Models;
using SlipSnap.Services;

namespace SlipSnap.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _sut;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SlipSnapTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var logger = NullLoggerFactory.Instance.CreateLogger<SettingsService>();
        _sut = new SettingsService(logger, _tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultsAndCreatesFile()
    {
        var settings = _sut.Load();

        settings.Should().NotBeNull();
        settings.Theme.Should().Be(ThemeMode.Auto);
        settings.OpacityPercent.Should().Be(80);
        settings.Toolbars.Should().HaveCount(4);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var original = AppSettings.CreateDefault();
        original.Theme = ThemeMode.Dark;
        original.OpacityPercent = 50;
        original.FullscreenOnly = true;

        _sut.Save(original);
        var loaded = _sut.Load();

        loaded.Theme.Should().Be(ThemeMode.Dark);
        loaded.OpacityPercent.Should().Be(50);
        loaded.FullscreenOnly.Should().BeTrue();
    }

    [Fact]
    public void Load_WhenFileIsCorrupt_ReturnsDefaults()
    {
        string settingsPath = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(settingsPath, "{{{{not valid json}}}}");

        var settings = _sut.Load();

        settings.Should().NotBeNull();
        settings.Theme.Should().Be(ThemeMode.Auto);
    }

    [Fact]
    public void Load_WhenFileIsEmpty_ReturnsDefaults()
    {
        string settingsPath = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(settingsPath, "");

        var settings = _sut.Load();

        settings.Should().NotBeNull();
        settings.OpacityPercent.Should().Be(80);
    }

    [Fact]
    public void Save_RaisesSettingsChangedEvent()
    {
        AppSettings? received = null;
        _sut.SettingsChanged += (_, s) => received = s;

        var settings = AppSettings.CreateDefault();
        _sut.Save(settings);

        received.Should().NotBeNull();
    }
}
