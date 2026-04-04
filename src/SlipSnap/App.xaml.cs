using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;
using Serilog;
using SlipSnap.Models;
using SlipSnap.Services;
using SlipSnap.ViewModels;
using SlipSnap.Views;
using Wpf.Ui.Appearance;
using Application = System.Windows.Application;

namespace SlipSnap;

public partial class App : Application
{
    private ILoggerFactory _loggerFactory = null!;
    private ILogger<App> _logger = null!;
    private SettingsService _settingsService = null!;
    private AppSettings _settings = null!;
    private ToolbarManager _toolbarManager = null!;
    private KeyboardSimulator _keyboardSimulator = null!;
    private VirtualDesktopService _virtualDesktopService = null!;
    private FullscreenDetector _fullscreenDetector = null!;
    private RdpSessionDetector _rdpSessionDetector = null!;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers
        DispatcherUnhandledException += (_, args) =>
        {
            Log.Fatal(args.Exception, "Unhandled dispatcher exception");
            Log.CloseAndFlush();
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Unhandled domain exception");
                Log.CloseAndFlush();
            }
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Fatal(args.Exception, "Unobserved task exception");
            Log.CloseAndFlush();
        };

        // Configure Serilog
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SlipSnap", "logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDir, "slipsnap-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10 * 1024 * 1024)
            .CreateLogger();

        _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddSerilog(dispose: true));
        _logger = _loggerFactory.CreateLogger<App>();
        _logger.LogInformation("SlipSnap starting");

        // Load settings
        _settingsService = new SettingsService(_loggerFactory.CreateLogger<SettingsService>());
        _settings = _settingsService.Load();

        // Apply theme
        ApplyTheme(_settings.Theme);

        // Handle --settings flag: open settings dialog only, then exit
        var args = Environment.GetCommandLineArgs();
        if (args.Contains("--settings", StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Opening settings via --settings flag");
            OpenSettings();
            _logger.LogInformation("Settings closed, shutting down");
            Log.CloseAndFlush();
            _loggerFactory.Dispose();
            Shutdown();
            return;
        }

        // Create services
        _keyboardSimulator = new KeyboardSimulator();
        _virtualDesktopService = new VirtualDesktopService(_loggerFactory.CreateLogger<VirtualDesktopService>());
        _fullscreenDetector = new FullscreenDetector(_loggerFactory.CreateLogger<FullscreenDetector>());
        _rdpSessionDetector = new RdpSessionDetector();

        _logger.LogInformation("Virtual desktop API available: {Available}", _virtualDesktopService.IsAvailable);
        _logger.LogInformation("RDP session: {IsRdp}", _rdpSessionDetector.IsRdpSession);

        // Create and apply toolbar manager
        _toolbarManager = new ToolbarManager(_keyboardSimulator, _virtualDesktopService,
            _loggerFactory.CreateLogger<ToolbarManager>());
        _toolbarManager.ApplySettings(_settings);

        // Wire fullscreen detection to toolbar visibility
        _fullscreenDetector.FullscreenStateChanged += (_, isFullscreen) =>
        {
            Dispatcher.Invoke(() =>
                _toolbarManager.UpdateVisibility(isFullscreen, _rdpSessionDetector.IsRdpSession));
        };
        _fullscreenDetector.StartMonitoring();

        // Initial visibility check
        _toolbarManager.UpdateVisibility(
            _fullscreenDetector.IsFullscreenWindowPresent,
            _rdpSessionDetector.IsRdpSession);

        // Subscribe to settings changes for live updates
        _settingsService.SettingsChanged += (_, newSettings) =>
        {
            _settings = newSettings;
            Dispatcher.Invoke(() =>
            {
                ApplyTheme(newSettings.Theme);
                _toolbarManager.ApplySettings(newSettings);
                _toolbarManager.UpdateVisibility(
                    _fullscreenDetector.IsFullscreenWindowPresent,
                    _rdpSessionDetector.IsRdpSession);
            });
        };

        // Create tray icon
        SetupTrayIcon();

        _logger.LogInformation("SlipSnap started with {EnabledCount} toolbar(s)",
            _settings.Toolbars.Count(t => t.Value.IsEnabled));

        // Check UIAccess status
        CheckUIAccessStatus();
    }

    private void CheckUIAccessStatus()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            // UIAccess tokens have elevated integrity level; we check if we're running from Program Files
            var exePath = Environment.ProcessPath ?? string.Empty;
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            bool isFromProgramFiles = exePath.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation("UIAccess check: running from {Path}, Program Files = {IsFromPF}",
                exePath, isFromProgramFiles);

            if (!isFromProgramFiles)
            {
                _logger.LogWarning("App not running from Program Files; UIAccess may not be active. " +
                    "Toolbars may not appear above fullscreen windows.");
                _notifyIcon?.ShowBalloonTip(5000, "SlipSnap",
                    "Not running from Program Files — toolbars may not overlay fullscreen windows.",
                    System.Windows.Forms.ToolTipIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check UIAccess status");
        }
    }

    private void SetupTrayIcon()
    {
        var iconStream = GetResourceStream(new Uri("pack://application:,,,/Assets/slipsnap.ico"))?.Stream;
        var icon = iconStream != null
            ? new System.Drawing.Icon(iconStream)
            : SystemIcons.Application;

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "SlipSnap",
            Icon = icon,
            Visible = true,
            ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip()
        };

        _notifyIcon.ContextMenuStrip.Items.Add("Settings", null, (_, _) => OpenSettings());
        _notifyIcon.ContextMenuStrip.Items.Add("About", null, (_, _) => ShowAbout());
        _notifyIcon.ContextMenuStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add("Quit", null, (_, _) => QuitApp());
    }

    private void ShowAbout()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version is not null ? $"{version.Major}.{version.Minor}.{version.Build}" : "dev";
        System.Windows.MessageBox.Show(
            $"SlipSnap v{versionStr}\n\nA floating toolbar for Windows that stays on top of fullscreen applications.",
            "About SlipSnap",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenSettings()
    {
        _logger.LogInformation("Opening settings dialog");
        var vm = new SettingsViewModel(_settingsService);
        var window = new SettingsWindow(vm);
        window.ShowDialog();
    }

    private void QuitApp()
    {
        _logger.LogInformation("Quit requested from tray icon");
        _toolbarManager?.CloseAll();
        Current.Shutdown();
    }

    internal void ApplyTheme(ThemeMode theme)
    {
        var wpfTheme = theme switch
        {
            ThemeMode.Light => ApplicationTheme.Light,
            ThemeMode.Dark => ApplicationTheme.Dark,
            _ => ApplicationTheme.Dark // Auto defaults to Dark; updated in Phase 7
        };
        ApplicationThemeManager.Apply(wpfTheme);
    }

    internal ILoggerFactory LoggerFactory => _loggerFactory;
    internal SettingsService SettingsService => _settingsService;
    internal AppSettings Settings => _settings;
    internal ToolbarManager ToolbarManager => _toolbarManager;

    protected override void OnExit(ExitEventArgs e)
    {
        _logger.LogInformation("SlipSnap shutting down");
        _fullscreenDetector?.Dispose();
        _notifyIcon?.Dispose();
        _toolbarManager?.CloseAll();
        Log.CloseAndFlush();
        _loggerFactory.Dispose();
        base.OnExit(e);
    }
}

