using System.ComponentModel;
using System.Runtime.CompilerServices;
using SlipSnap.Models;
using SlipSnap.Services;

namespace SlipSnap.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = settingsService.Load();

        LeftButtons = new ToolbarEdgeButtonsViewModel(_settings.Toolbars[ToolbarEdge.Left], SaveSettings);
        RightButtons = new ToolbarEdgeButtonsViewModel(_settings.Toolbars[ToolbarEdge.Right], SaveSettings);
        TopButtons = new ToolbarEdgeButtonsViewModel(_settings.Toolbars[ToolbarEdge.Top], SaveSettings);
        BottomButtons = new ToolbarEdgeButtonsViewModel(_settings.Toolbars[ToolbarEdge.Bottom], SaveSettings);
    }

    public ThemeMode Theme
    {
        get => _settings.Theme;
        set
        {
            if (_settings.Theme == value) return;
            _settings.Theme = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public int OpacityPercent
    {
        get => _settings.OpacityPercent;
        set
        {
            int clamped = Math.Clamp(value, 10, 100);
            if (_settings.OpacityPercent == clamped) return;
            _settings.OpacityPercent = clamped;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool FullscreenOnly
    {
        get => _settings.FullscreenOnly;
        set
        {
            if (_settings.FullscreenOnly == value) return;
            _settings.FullscreenOnly = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool HideInRdpSession
    {
        get => _settings.HideInRdpSession;
        set
        {
            if (_settings.HideInRdpSession == value) return;
            _settings.HideInRdpSession = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool TouchMode
    {
        get => _settings.TouchMode;
        set
        {
            if (_settings.TouchMode == value) return;
            _settings.TouchMode = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool IsLeftEnabled
    {
        get => _settings.Toolbars[ToolbarEdge.Left].IsEnabled;
        set { _settings.Toolbars[ToolbarEdge.Left].IsEnabled = value; OnPropertyChanged(); SaveSettings(); }
    }

    public bool IsRightEnabled
    {
        get => _settings.Toolbars[ToolbarEdge.Right].IsEnabled;
        set { _settings.Toolbars[ToolbarEdge.Right].IsEnabled = value; OnPropertyChanged(); SaveSettings(); }
    }

    public bool IsTopEnabled
    {
        get => _settings.Toolbars[ToolbarEdge.Top].IsEnabled;
        set { _settings.Toolbars[ToolbarEdge.Top].IsEnabled = value; OnPropertyChanged(); SaveSettings(); }
    }

    public bool IsBottomEnabled
    {
        get => _settings.Toolbars[ToolbarEdge.Bottom].IsEnabled;
        set { _settings.Toolbars[ToolbarEdge.Bottom].IsEnabled = value; OnPropertyChanged(); SaveSettings(); }
    }

    // Per-edge button selection helpers
    public ToolbarEdgeButtonsViewModel LeftButtons { get; }
    public ToolbarEdgeButtonsViewModel RightButtons { get; }
    public ToolbarEdgeButtonsViewModel TopButtons { get; }
    public ToolbarEdgeButtonsViewModel BottomButtons { get; }

    private void SaveSettings()
    {
        _settingsService.Save(_settings);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class ToolbarEdgeButtonsViewModel : INotifyPropertyChanged
{
    private readonly ToolbarConfig _config;
    private readonly Action _save;

    public ToolbarEdgeButtonsViewModel(ToolbarConfig config, Action save)
    {
        _config = config;
        _save = save;
    }

    public bool HasStartMenu
    {
        get => _config.Buttons.Contains(ToolbarButtonType.StartMenu);
        set { ToggleButton(ToolbarButtonType.StartMenu, value); OnPropertyChanged(); }
    }

    public bool HasTaskView
    {
        get => _config.Buttons.Contains(ToolbarButtonType.TaskView);
        set { ToggleButton(ToolbarButtonType.TaskView, value); OnPropertyChanged(); }
    }

    public bool HasNextDesktop
    {
        get => _config.Buttons.Contains(ToolbarButtonType.NextDesktop);
        set { ToggleButton(ToolbarButtonType.NextDesktop, value); OnPropertyChanged(); }
    }

    public bool HasPrevDesktop
    {
        get => _config.Buttons.Contains(ToolbarButtonType.PrevDesktop);
        set { ToggleButton(ToolbarButtonType.PrevDesktop, value); OnPropertyChanged(); }
    }

    private void ToggleButton(ToolbarButtonType type, bool include)
    {
        if (include && !_config.Buttons.Contains(type))
        {
            _config.Buttons.Add(type);
        }
        else if (!include)
        {
            _config.Buttons.Remove(type);
        }
        _save();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
