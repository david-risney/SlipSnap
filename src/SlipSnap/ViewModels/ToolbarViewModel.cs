using System.Windows.Input;
using SlipSnap.Interop;
using SlipSnap.Services;

namespace SlipSnap.ViewModels;

public class ToolbarViewModel
{
    private readonly IKeyboardSimulator _keyboard;

    public ToolbarViewModel(IKeyboardSimulator keyboard, bool virtualDesktopAvailable = true)
    {
        _keyboard = keyboard;

        NextDesktopCommand = new RelayCommand(
            () => _keyboard.SendKeys(VirtualKey.VK_LCONTROL, VirtualKey.VK_LWIN, VirtualKey.VK_RIGHT),
            () => virtualDesktopAvailable);

        PrevDesktopCommand = new RelayCommand(
            () => _keyboard.SendKeys(VirtualKey.VK_LCONTROL, VirtualKey.VK_LWIN, VirtualKey.VK_LEFT),
            () => virtualDesktopAvailable);

        StartMenuCommand = new RelayCommand(() =>
            _keyboard.SendKeys(VirtualKey.VK_LWIN));

        TaskViewCommand = new RelayCommand(() =>
            _keyboard.SendKeys(VirtualKey.VK_LWIN, VirtualKey.VK_TAB));

        VirtualDesktopTooltip = virtualDesktopAvailable
            ? null
            : "Virtual desktop API unavailable";
    }

    public ICommand NextDesktopCommand { get; }
    public ICommand PrevDesktopCommand { get; }
    public ICommand StartMenuCommand { get; }
    public ICommand TaskViewCommand { get; }
    public string? VirtualDesktopTooltip { get; }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}
