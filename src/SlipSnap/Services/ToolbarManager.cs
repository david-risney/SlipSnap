using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SlipSnap.Models;
using SlipSnap.ViewModels;
using SlipSnap.Views;

namespace SlipSnap.Services;

public class ToolbarManager : IToolbarManager
{
    private readonly IKeyboardSimulator _keyboard;
    private readonly IVirtualDesktopService _virtualDesktop;
    private readonly ILogger<ToolbarManager> _logger;
    private readonly Dictionary<ToolbarEdge, ToolbarWindow> _windows = new();
    private AppSettings? _currentSettings;

    public ToolbarManager(
        IKeyboardSimulator keyboard,
        IVirtualDesktopService virtualDesktop,
        ILogger<ToolbarManager> logger)
    {
        _keyboard = keyboard;
        _virtualDesktop = virtualDesktop;
        _logger = logger;

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        _logger.LogInformation("Display settings changed, repositioning toolbars");
        foreach (var window in _windows.Values)
        {
            window.SnapToEdge();
        }
    }

    public void ApplySettings(AppSettings settings)
    {
        _currentSettings = settings;

        // Determine which edges need toolbars
        var desiredEdges = new HashSet<ToolbarEdge>();
        foreach (var (edge, config) in settings.Toolbars)
        {
            if (config.IsEnabled)
            {
                desiredEdges.Add(edge);
            }
        }

        // Remove toolbars that are no longer needed
        var toRemove = _windows.Keys.Where(e => !desiredEdges.Contains(e)).ToList();
        foreach (var edge in toRemove)
        {
            _logger.LogDebug("Closing toolbar on {Edge}", edge);
            _windows[edge].Close();
            _windows.Remove(edge);
        }

        // Create or update toolbars
        foreach (var edge in desiredEdges)
        {
            if (!_windows.ContainsKey(edge))
            {
                _logger.LogDebug("Creating toolbar on {Edge}", edge);
                var window = CreateToolbarWindow(edge, settings);
                _windows[edge] = window;
                window.Show();

                // Pin to all virtual desktops
                if (window.Hwnd != IntPtr.Zero)
                {
                    _virtualDesktop.PinWindow(window.Hwnd);
                }
            }
            else
            {
                // Update existing window
                var window = _windows[edge];
                window.PositionPercent = settings.Toolbars[edge].PositionPercent;
                window.ApplyOpacity(settings.OpacityPercent);
                window.SnapToEdge();
            }
        }
    }

    public void UpdateVisibility(bool isFullscreenPresent, bool isRdpSession)
    {
        if (_currentSettings is null) return;

        foreach (var (edge, window) in _windows)
        {
            var config = _currentSettings.Toolbars[edge];
            bool shouldBeVisible = EvaluateVisibility(config, isFullscreenPresent, isRdpSession);

            if (shouldBeVisible && !window.IsVisible)
            {
                window.Show();
            }
            else if (!shouldBeVisible && window.IsVisible)
            {
                window.Hide();
            }
        }
    }

    private bool EvaluateVisibility(ToolbarConfig config, bool isFullscreenPresent, bool isRdpSession)
    {
        if (_currentSettings is null) return false;

        // Visibility state machine from data-model.md
        // 1. RDP check
        if (_currentSettings.HideInRdpSession && isRdpSession) return false;
        // 2. Enabled check
        if (!config.IsEnabled) return false;
        // 3. Fullscreen-only check
        if (_currentSettings.FullscreenOnly && !isFullscreenPresent) return false;
        // 4. Otherwise visible
        return true;
    }

    public void CloseAll()
    {
        _logger.LogDebug("Closing all toolbars");
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        foreach (var window in _windows.Values)
        {
            window.Close();
        }
        _windows.Clear();
    }

    private ToolbarWindow CreateToolbarWindow(ToolbarEdge edge, AppSettings settings)
    {
        var vm = new ToolbarViewModel(_keyboard, _virtualDesktop.IsAvailable);
        var window = new ToolbarWindow
        {
            DataContext = vm,
            Edge = edge,
            PositionPercent = settings.Toolbars[edge].PositionPercent,
        };
        window.ApplyOpacity(settings.OpacityPercent);

        window.PositionChanged += OnToolbarPositionChanged;

        return window;
    }

    private void OnToolbarPositionChanged(ToolbarEdge edge, double positionPercent)
    {
        if (_currentSettings is null) return;
        if (_currentSettings.Toolbars.TryGetValue(edge, out var config))
        {
            config.PositionPercent = positionPercent;
            _logger.LogDebug("Toolbar {Edge} position changed to {Percent:F2}", edge, positionPercent);
        }
    }

    internal IReadOnlyDictionary<ToolbarEdge, ToolbarWindow> Windows => _windows;
}
