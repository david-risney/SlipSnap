using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WindowsDesktop;

namespace SlipSnap;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const byte VK_LWIN = 0x5B;
    private const byte VK_TAB = 0x09;
    private const byte VK_LCONTROL = 0xA2;
    private const byte VK_LEFT = 0x25;
    private const byte VK_RIGHT = 0x27;

    private IntPtr _hwnd;
    private bool _blockActivation;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;

        // Handle WM_MOUSEACTIVATE conditionally
        var source = HwndSource.FromHwnd(_hwnd);
        source?.AddHook(WndProc);

        // Snap to right edge of primary screen
        var screen = SystemParameters.WorkArea;
        Left = screen.Right - Width;
        Top = screen.Top + (screen.Height - Height) / 2;

        // Pin to all virtual desktops
        try
        {
            this.Pin();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to pin to all desktops: {ex.Message}");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_MOUSEACTIVATE = 0x0021;
        const int MA_NOACTIVATE = 0x0003;

        if (msg == WM_MOUSEACTIVATE && _blockActivation)
        {
            handled = true;
            return (IntPtr)MA_NOACTIVATE;
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// After switching host desktops, if we have focus and there's a fullscreen
    /// window on the new desktop, give it focus instead of keeping SlipSnap focused.
    /// </summary>
    private void FocusFullscreenWindowIfPresent()
    {
        // Use Dispatcher to run after the desktop switch has settled
        Dispatcher.InvokeAsync(() =>
        {
            var fg = GetForegroundWindow();
            if (fg != _hwnd) return; // Something else already has focus

            // Enumerate top-level windows to find a fullscreen one
            EnumWindows((hWnd, _) =>
            {
                if (hWnd == _hwnd) return true; // skip ourselves
                if (!IsWindowVisible(hWnd)) return true;

                if (GetWindowRect(hWnd, out var rect))
                {
                    // Get the monitor this window is on
                    var monitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
                    var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                    if (GetMonitorInfo(monitor, ref mi))
                    {
                        // Check if window covers the entire monitor
                        if (rect.Left <= mi.rcMonitor.Left &&
                            rect.Top <= mi.rcMonitor.Top &&
                            rect.Right >= mi.rcMonitor.Right &&
                            rect.Bottom >= mi.rcMonitor.Bottom)
                        {
                            SetForegroundWindow(hWnd);
                            return false; // stop enumerating
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private static void SendDesktopSwitch(byte directionKey)
    {
        keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(directionKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(directionKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    // --- Host buttons: take focus so keystrokes go to host shell ---

    private void StartMenu_Click(object sender, RoutedEventArgs e)
    {
        _blockActivation = false;
        SetForegroundWindow(_hwnd);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private void TaskView_Click(object sender, RoutedEventArgs e)
    {
        _blockActivation = false;
        SetForegroundWindow(_hwnd);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(VK_TAB, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private void PrevDesktop_Click(object sender, RoutedEventArgs e)
    {
        _blockActivation = false;
        SetForegroundWindow(_hwnd);
        SendDesktopSwitch(VK_LEFT);
        FocusFullscreenWindowIfPresent();
    }

    private void NextDesktop_Click(object sender, RoutedEventArgs e)
    {
        _blockActivation = false;
        SetForegroundWindow(_hwnd);
        SendDesktopSwitch(VK_RIGHT);
        FocusFullscreenWindowIfPresent();
    }

    // --- Remote buttons: block activation so RDP keeps focus ---

    private void RemotePrevDesktop_Click(object sender, RoutedEventArgs e)
    {
        _blockActivation = true;
        SendDesktopSwitch(VK_LEFT);
    }

    private void RemoteNextDesktop_Click(object sender, RoutedEventArgs e)
    {
        _blockActivation = true;
        SendDesktopSwitch(VK_RIGHT);
    }
}