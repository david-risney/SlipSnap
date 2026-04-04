using System.Text.Json;
using FluentAssertions;
using SlipSnap.E2ETests.Helpers;

namespace SlipSnap.E2ETests;

[Collection("App")]
public class SettingsTests
{
    private readonly AppFixture _app;

    public SettingsTests(AppFixture app) => _app = app;

    [Fact]
    public void Settings_ShouldOpen()
    {
        using var result = ScriptRunner.Run("open-settings.ps1");
        result.RootElement.GetProperty("settingsWindowOpen").GetBoolean().Should().BeTrue(
            "open-settings.ps1 should launch a settings instance and find the settings window");
    }

    [Fact]
    public void Settings_ShouldHaveThemeRadioButtons()
    {
        EnsureSettingsOpen();

        using var result = ScriptRunner.Run("inspect-settings.ps1");
        var radios = result.RootElement.GetProperty("radioButtons").EnumerateArray()
            .Select(r => r.GetProperty("name").GetString()!)
            .ToList();

        radios.Should().Contain("Auto");
        radios.Should().Contain("Light");
        radios.Should().Contain("Dark");
    }

    [Fact]
    public void Settings_ShouldHaveOpacitySlider()
    {
        EnsureSettingsOpen();

        using var result = ScriptRunner.Run("inspect-settings.ps1");
        var sliders = result.RootElement.GetProperty("sliders").EnumerateArray().ToList();

        sliders.Should().NotBeEmpty();
        var slider = sliders[0];
        slider.GetProperty("minimum").GetDouble().Should().Be(10);
        slider.GetProperty("maximum").GetDouble().Should().Be(100);
    }

    [Fact]
    public void Settings_ShouldHaveEdgeCheckboxes()
    {
        EnsureSettingsOpen();

        using var result = ScriptRunner.Run("inspect-settings.ps1");
        var checkboxes = result.RootElement.GetProperty("checkboxes").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString()!)
            .ToList();

        checkboxes.Should().Contain("Left edge");
        checkboxes.Should().Contain("Right edge");
        checkboxes.Should().Contain("Top edge");
        checkboxes.Should().Contain("Bottom edge");
    }

    private static void EnsureSettingsOpen()
    {
        // Check if settings is already open
        try
        {
            using var check = ScriptRunner.Run("inspect-settings.ps1");
            if (check.RootElement.TryGetProperty("checkboxes", out _))
                return; // Settings already open
        }
        catch { /* not open, open it */ }

        ScriptRunner.Run("open-settings.ps1");
    }
}
