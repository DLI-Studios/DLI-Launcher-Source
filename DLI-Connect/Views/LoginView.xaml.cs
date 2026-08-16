using System.Windows;
using System.Windows.Controls;
using DLI.Connect.ViewModels;

namespace DLI.Connect.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (DataContext is LoginViewModel vm)
            {
                PasswordBox.PasswordChanged += (_, _) => vm.Password = PasswordBox.Password;
                PasswordBox.Focus();
            }
        };

        PasswordToggle.Click += (_, _) =>
        {
            var hidden = PasswordBox.PasswordChar == '●';
            PasswordBox.PasswordChar = hidden ? '\0' : '●';
            PasswordToggleIcon.Text = hidden ? "\uE890" : "\uE7B3";
            PasswordToggleIcon.Foreground = hidden
                ? (System.Windows.Media.Brush)Application.Current.FindResource("AccentBrush")
                : (System.Windows.Media.Brush)Application.Current.FindResource("TextMutedBrush");
        };
    }
}
