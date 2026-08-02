using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DLI.Connect.ViewModels;

namespace DLI.Connect.Views;

public partial class RegisterView : UserControl
{
    public RegisterView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (DataContext is RegisterViewModel vm)
            {
                PasswordBox.PasswordChanged += (_, _) => vm.Password = PasswordBox.Password;
                ConfirmPasswordBox.PasswordChanged += (_, _) => vm.ConfirmPassword = ConfirmPasswordBox.Password;
            }
        };

        AttachToggle(PasswordToggle, PasswordToggleIcon, PasswordBox);
        AttachToggle(ConfirmPasswordToggle, ConfirmPasswordToggleIcon, ConfirmPasswordBox);
    }

    private static void AttachToggle(Button toggle, TextBlock icon, PasswordBox box)
    {
        toggle.Click += (_, _) =>
        {
            var hidden = box.PasswordChar == '●';
            box.PasswordChar = hidden ? '\0' : '●';
            icon.Text = hidden ? "\uE890" : "\uE7B3";
            icon.Foreground = hidden
                ? (Brush)Application.Current.FindResource("AccentBrush")
                : (Brush)Application.Current.FindResource("TextMutedBrush");
        };
    }
}
