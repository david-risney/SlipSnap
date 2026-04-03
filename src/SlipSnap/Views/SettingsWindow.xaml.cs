using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Wpf.Ui.Controls;
using SlipSnap.ViewModels;
using SlipSnap.Models;
using Wpf.Ui.Appearance;

namespace SlipSnap.Views;

public partial class SettingsWindow : FluentWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.Theme))
            {
                var app = (App)System.Windows.Application.Current;
                app.ApplyTheme(viewModel.Theme);
            }
        };
    }
}

public class EnumBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.Equals(parameter) ?? false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? parameter : DependencyProperty.UnsetValue;
    }
}
