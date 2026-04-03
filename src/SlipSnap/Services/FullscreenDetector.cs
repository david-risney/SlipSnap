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

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (hWnd == _selfHwnd) return true; // Skip our own windows
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            if (NativeMethods.GetWindowRect(hWnd, out var rect))
            {
                var monitor = NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
                var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };

                if (NativeMethods.GetMonitorInfo(monitor, ref mi))
                {
                    if (rect.Left <= mi.rcMonitor.Left &&
                        rect.Top <= mi.rcMonitor.Top &&
                        rect.Right >= mi.rcMonitor.Right &&
                        rect.Bottom >= mi.rcMonitor.Bottom)
                    {
                        found = true;
                        return false; // Stop enumerating
                    }
                }
            }
            return true;
        }, IntPtr.Zero);

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
