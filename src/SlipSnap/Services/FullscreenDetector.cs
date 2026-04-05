using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using SlipSnap.Interop;

namespace SlipSnap.Services;

public class FullscreenDetector : IFullscreenDetector, IDisposable
{
    private readonly ILogger<FullscreenDetector> _logger;
    private readonly IntPtr _selfHwnd;
    private IntPtr _hookHandle;
    private NativeMethods.WinEventDelegate? _winEventDelegate;
    private bool _lastState;

    public FullscreenDetector(ILogger<FullscreenDetector> logger, IntPtr selfHwnd = default)
    {
        _logger = logger;
        _selfHwnd = selfHwnd;
    }

    public bool IsFullscreenWindowPresent { get; private set; }

    public event EventHandler<bool>? FullscreenStateChanged;

    public void StartMonitoring()
    {
        if (_hookHandle != IntPtr.Zero) return;

        // Must keep delegate alive to prevent GC collection
        _winEventDelegate = OnWinEvent;

        _hookHandle = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _winEventDelegate,
            0, 0,
            NativeMethods.WINEVENT_OUTOFCONTEXT);

        if (_hookHandle == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to set WinEvent hook for fullscreen detection");
        }
        else
        {
            _logger.LogInformation("Fullscreen detection monitoring started");
        }

        // Initial check
        CheckFullscreen();
    }

    public void StopMonitoring()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_hookHandle);
            _hookHandle = IntPtr.Zero;
            _logger.LogInformation("Fullscreen detection monitoring stopped");
        }
        _winEventDelegate = null;
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        CheckFullscreen();
    }

    private void CheckFullscreen()
    {
        bool found = false;
        var fgHwnd = NativeMethods.GetForegroundWindow();

        if (fgHwnd != IntPtr.Zero && fgHwnd != _selfHwnd)
        {
            var shellHwnd = NativeMethods.GetShellWindow();
            if (fgHwnd != shellHwnd && NativeMethods.IsWindowVisible(fgHwnd))
            {
                // Skip known desktop-layer classes
                var classNameBuf = new char[64];
                int classLen = NativeMethods.GetClassName(fgHwnd, classNameBuf, classNameBuf.Length);
                var className = classLen > 0 ? new string(classNameBuf, 0, classLen) : string.Empty;
                bool isDesktopClass = className is "WorkerW" or "Progman"
                    or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";

                if (!isDesktopClass && NativeMethods.GetWindowRect(fgHwnd, out var rect))
                {
                    var monitor = NativeMethods.MonitorFromWindow(fgHwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
                    var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };

                    if (NativeMethods.GetMonitorInfo(monitor, ref mi))
                    {
                        found = rect.Left <= mi.rcMonitor.Left &&
                                rect.Top <= mi.rcMonitor.Top &&
                                rect.Right >= mi.rcMonitor.Right &&
                                rect.Bottom >= mi.rcMonitor.Bottom;
                    }
                }
            }
        }

        IsFullscreenWindowPresent = found;

        if (found != _lastState)
        {
            _lastState = found;
            _logger.LogDebug("Fullscreen state changed to {State}", found);
            FullscreenStateChanged?.Invoke(this, found);
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}
