namespace SlipSnap.Services;

public interface IVirtualDesktopService
{
    bool IsAvailable { get; }
    void PinWindow(IntPtr hwnd);
    void SwitchToNext();
    void SwitchToPrevious();
}
