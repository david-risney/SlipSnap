using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using SlipSnap.Interop;
using SlipSnap.Models;
using SlipSnap.ViewModels;
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

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;

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

        switch (_edge)
        {
            case ToolbarEdge.Left:
                Width = 48;
                Height = 260;
                Left = screen.Left;
                Top = screen.Top + (screen.Height - Height) * _positionPercent;
                break;
            case ToolbarEdge.Right:
                Width = 48;
                Height = 260;
                Left = screen.Right - Width;
                Top = screen.Top + (screen.Height - Height) * _positionPercent;
                break;
            case ToolbarEdge.Top:
                Width = 260;
                Height = 48;
                Left = screen.Left + (screen.Width - Width) * _positionPercent;
                Top = screen.Top;
                break;
            case ToolbarEdge.Bottom:
                Width = 260;
                Height = 48;
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
    }

    // --- Grip drag ---

    private void Grip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragStart = e.GetPosition(this);
        ((UIElement)sender).CaptureMouse();
    }

    private void Grip_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var screen = SystemParameters.WorkArea;
        var position = e.GetPosition(null);

        // Convert to screen coordinates
        var screenPoint = PointToScreen(position);

        bool isVertical = _edge is ToolbarEdge.Left or ToolbarEdge.Right;
        if (isVertical)
        {
            double newTop = screenPoint.Y - _dragStart.Y;
            newTop = Math.Clamp(newTop, screen.Top, screen.Bottom - Height);
            Top = newTop;
            _positionPercent = (screen.Height - Height) > 0
                ? (Top - screen.Top) / (screen.Height - Height)
                : 0.5;
        }
        else
        {
            double newLeft = screenPoint.X - _dragStart.X;
            newLeft = Math.Clamp(newLeft, screen.Left, screen.Right - Width);
            Left = newLeft;
            _positionPercent = (screen.Width - Width) > 0
                ? (Left - screen.Left) / (screen.Width - Width)
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
