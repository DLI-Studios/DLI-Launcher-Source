using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DLI.Connect.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void PushToTalkKeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        if (DataContext is not ViewModels.SettingsViewModel vm) return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt)
        {
            return;
        }

        vm.PushToTalkKey = key == Key.Escape ? "" : key.ToString();
    }

    private void OnCurrentPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm && sender is PasswordBox box)
        {
            vm.CurrentPassword = box.Password;
        }
    }

    private void OnNewPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm && sender is PasswordBox box)
        {
            vm.NewPassword = box.Password;
        }
    }

    private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm && sender is PasswordBox box)
        {
            vm.ConfirmPassword = box.Password;
        }
    }
}
