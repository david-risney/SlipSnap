using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using SlipSnap.Interop;
using SlipSnap.Models;
using SlipSnap.ViewModels;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace SlipSnap.Views;

public partial class ToolbarWindow : Window
{
    private IntPtr _hwnd;
    private bool _blockActivation = true;
    private bool _isDragging;
    private Point _dragStart;
    private ToolbarEdge _edge = ToolbarEdge.Left;
    private double _positionPercent = 0.5;

    public ToolbarWindow()
    {
        InitializeComponent();
        ApplyThemeColors(ThemeMode.Dark);
    }

    public ToolbarEdge Edge
    {
        get => _edge;
        set
        {
            _edge = value;
            UpdateLayoutForEdge();
        }
    }

    public double PositionPercent
    {
        get => _positionPercent;
        set => _positionPercent = Math.Clamp(value, 0.0, 1.0);
    }

    public event Action<ToolbarEdge, double>? PositionChanged;

    public void ApplyOpacity(int opacityPercent)
    {
        Opacity = Math.Clamp(opacityPercent, 10, 100) / 100.0;
    }

    private bool _touchMode;

    public void ApplyTouchMode(bool touchMode)
    {
        _touchMode = touchMode;
        double fontSize = touchMode ? 24 : 18;
        var margin = touchMode ? new Thickness(4) : new Thickness(2);
        var padding = touchMode ? new Thickness(8) : new Thickness(4);

        double size = touchMode ? 48 : 32;
        foreach (var btn in new[] { BtnStartMenu, BtnTaskView, BtnPrevDesktop, BtnNextDesktop })
        {
            btn.FontSize = fontSize;
            btn.Margin = margin;
            btn.Padding = padding;
            btn.Width = size;
            btn.Height = size;
        }

        GripArea.Margin = margin;
        GripText.FontSize = touchMode ? 14 : 10;
        UpdateLayoutForEdge();
    }

    public void UpdateButtons(IList<ToolbarButtonType> buttons)
    {
        BtnStartMenu.Visibility = buttons.Contains(ToolbarButtonType.StartMenu)
            ? Visibility.Visible : Visibility.Collapsed;
        BtnTaskView.Visibility = buttons.Contains(ToolbarButtonType.TaskView)
            ? Visibility.Visible : Visibility.Collapsed;
        BtnPrevDesktop.Visibility = buttons.Contains(ToolbarButtonType.PrevDesktop)
            ? Visibility.Visible : Visibility.Collapsed;
        BtnNextDesktop.Visibility = buttons.Contains(ToolbarButtonType.NextDesktop)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ApplyThemeColors(ThemeMode theme)
    {
        bool isDark = theme != ThemeMode.Light;
        var bg = isDark ? Color.FromRgb(0x2B, 0x2B, 0x2B) : Color.FromRgb(0xF3, 0xF3, 0xF3);
        var accent = isDark ? Color.FromRgb(0x44, 0x44, 0x44) : Color.FromRgb(0xD0, 0xD0, 0xD0);
        var fg = isDark ? Colors.LightGray : Colors.DimGray;
        var btnBg = isDark ? Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x55, 0x00, 0x00, 0x00);
        var btnFg = isDark ? Colors.White : Color.FromRgb(0x1A, 0x1A, 0x1A);

        ToolbarBorder.Background = new SolidColorBrush(bg);
        GripArea.Background = new SolidColorBrush(accent);
        GripText.Foreground = new SolidColorBrush(fg);

        var btnStyle = new Style(typeof(System.Windows.Controls.Button), (Style)FindResource("OverlayButtonStyle"));
        btnStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(btnFg)));
        btnStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(btnBg)));
        Resources["OverlayButtonStyle"] = btnStyle;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;

        // Hide from Alt+Tab / Task View
        var exStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE,
            exStyle | NativeMethods.WS_EX_TOOLWINDOW);

        // Hook WndProc for non-activation
        var source = HwndSource.FromHwnd(_hwnd);
        source?.AddHook(WndProc);

        SnapToEdge();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_MOUSEACTIVATE && _blockActivation)
        {
            handled = true;
            return (IntPtr)NativeMethods.MA_NOACTIVATE;
        }
        return IntPtr.Zero;
    }

    public void SnapToEdge()
    {
        var screen = SystemParameters.WorkArea;
        bool isVertical = _edge is ToolbarEdge.Left or ToolbarEdge.Right;

        // Count visible buttons
        int visibleButtons = new[] { BtnStartMenu, BtnTaskView, BtnPrevDesktop, BtnNextDesktop }
            .Count(b => b.Visibility == Visibility.Visible);

        // Calculate size based on touch mode and button count
        double btnSize = _touchMode ? 56 : 36;  // button size + margin
        double gripSize = _touchMode ? 32 : 20;  // grip + margin
        double padding = 16;  // border padding (4*2) + some breathing room
        double contentLength = gripSize + (visibleButtons * btnSize) + padding;
        double barThickness = _touchMode ? 64 : 48;

        if (isVertical)
        {
            Width = barThickness;
            Height = contentLength;
        }
        else
        {
            Width = contentLength;
            Height = barThickness;
        }

        switch (_edge)
        {
            case ToolbarEdge.Left:
                Left = screen.Left;
                Top = screen.Top + (screen.Height - Height) * _positionPercent;
                break;
            case ToolbarEdge.Right:
                Left = screen.Right - Width;
                Top = screen.Top + (screen.Height - Height) * _positionPercent;
                break;
            case ToolbarEdge.Top:
                Left = screen.Left + (screen.Width - Width) * _positionPercent;
                Top = screen.Top;
                break;
            case ToolbarEdge.Bottom:
                Left = screen.Left + (screen.Width - Width) * _positionPercent;
                Top = screen.Bottom - Height;
                break;
        }
    }

    private void UpdateLayoutForEdge()
    {
        if (ButtonPanel is null) return;

        bool isVertical = _edge is ToolbarEdge.Left or ToolbarEdge.Right;
        ButtonPanel.Orientation = isVertical
            ? System.Windows.Controls.Orientation.Vertical
            : System.Windows.Controls.Orientation.Horizontal;

        // Grip: same width as buttons but half height (vertical), swapped for horizontal
        double btnSize = _touchMode ? 48 : 32;
        double gripHalf = Math.Round(btnSize / 2);
        if (isVertical)
        {
            GripArea.Width = btnSize;
            GripArea.Height = gripHalf;
            GripText.Text = "\u22EF"; // horizontal dots
        }
        else
        {
            GripArea.Width = gripHalf;
            GripArea.Height = btnSize;
            GripText.Text = "\u22EE"; // vertical dots
        }
    }

    // --- Grip drag ---

    private void Grip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        // Capture mouse position relative to the window (logical/WPF units)
        _dragStart = e.GetPosition(this);
        ((UIElement)sender).CaptureMouse();
    }

    private void Grip_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var screen = SystemParameters.WorkArea;

        // Current mouse position relative to the window (logical/WPF units)
        var current = e.GetPosition(this);

        // Compute delta in logical units (both points are relative to the window)
        double deltaX = current.X - _dragStart.X;
        double deltaY = current.Y - _dragStart.Y;

        bool isVertical = _edge is ToolbarEdge.Left or ToolbarEdge.Right;
        if (isVertical)
        {
            double newTop = Top + deltaY;
            newTop = Math.Clamp(newTop, screen.Top, screen.Bottom - ActualHeight);
            Top = newTop;
            _positionPercent = (screen.Height - ActualHeight) > 0
                ? (Top - screen.Top) / (screen.Height - ActualHeight)
                : 0.5;
        }
        else
        {
            double newLeft = Left + deltaX;
            newLeft = Math.Clamp(newLeft, screen.Left, screen.Right - ActualWidth);
            Left = newLeft;
            _positionPercent = (screen.Width - ActualWidth) > 0
                ? (Left - screen.Left) / (screen.Width - ActualWidth)
                : 0.5;
        }
    }

    private void Grip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        ((UIElement)sender).ReleaseMouseCapture();
        _positionPercent = Math.Clamp(_positionPercent, 0.0, 1.0);
        PositionChanged?.Invoke(_edge, _positionPercent);
    }

    public IntPtr Hwnd => _hwnd;
}
