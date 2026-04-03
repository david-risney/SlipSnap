using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using WindowsDesktop;

namespace SlipSnap.Services;

public class VirtualDesktopService : IVirtualDesktopService
{
    private readonly ILogger<VirtualDesktopService> _logger;

    public VirtualDesktopService(ILogger<VirtualDesktopService> logger)
    {
        _logger = logger;

        try
        {
            // Test whether the VirtualDesktop COM interface is available
            _ = VirtualDesktop.GetDesktops();
            IsAvailable = true;
            _logger.LogInformation("Virtual desktop API available");
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            _logger.LogWarning(ex, "Virtual desktop API not available on this OS build");
        }
    }

    public bool IsAvailable { get; }

    public void PinWindow(IntPtr hwnd)
    {
        if (!IsAvailable) return;

        try
        {
            VirtualDesktop.PinWindow(hwnd);
            _logger.LogDebug("Pinned window {Hwnd} to all desktops", hwnd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pin window {Hwnd}", hwnd);
        }
    }

    public void SwitchToNext()
    {
        if (!IsAvailable) return;

        try
        {
            var current = VirtualDesktop.Current;
            var next = current.GetRight();
            if (next is not null)
            {
                next.Switch();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to next desktop");
        }
    }

    public void SwitchToPrevious()
    {
        if (!IsAvailable) return;

        try
        {
            var current = VirtualDesktop.Current;
            var prev = current.GetLeft();
            if (prev is not null)
            {
                prev.Switch();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to previous desktop");
        }
    }
}
