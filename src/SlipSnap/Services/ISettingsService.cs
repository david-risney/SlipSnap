using SlipSnap.Models;

namespace SlipSnap.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
    event EventHandler<AppSettings>? SettingsChanged;
}
