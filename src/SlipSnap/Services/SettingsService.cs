using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SlipSnap.Models;

namespace SlipSnap.Services;

public class SettingsService : ISettingsService
{
    private static readonly string DefaultSettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SlipSnap");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsDir;
    private readonly string _settingsPath;

    public SettingsService(ILogger<SettingsService> logger, string? settingsDir = null)
    {
        _logger = logger;
        _settingsDir = settingsDir ?? DefaultSettingsDir;
        _settingsPath = Path.Combine(_settingsDir, "settings.json");
    }

    public event EventHandler<AppSettings>? SettingsChanged;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogInformation("Settings file not found at {Path}, creating defaults", _settingsPath);
                var defaults = AppSettings.CreateDefault();
                Save(defaults);
                return defaults;
            }

            string json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

            if (settings is null)
            {
                _logger.LogWarning("Settings file deserialized to null, returning defaults");
                return AppSettings.CreateDefault();
            }

            settings.Normalize();
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}, returning defaults", _settingsPath);
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_settingsDir);

            settings.Normalize();
            string json = JsonSerializer.Serialize(settings, JsonOptions);

            // Atomic write: write to temp file, then rename
            string tempPath = _settingsPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _settingsPath, overwrite: true);

            _logger.LogInformation("Settings saved to {Path}", _settingsPath);
            SettingsChanged?.Invoke(this, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _settingsPath);
            throw;
        }
    }
}
