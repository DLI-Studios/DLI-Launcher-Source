using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Services;
using DLI.Connect.Services.Interfaces;
using DLI.Connect.Utilities;

namespace DLI.Connect.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly ISessionManager _session;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private bool _rememberMe = true;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(ISessionManager session, INavigationService navigation)
    {
        _session = session;
        _navigation = navigation;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;

        if (!Validators.IsValidEmail(Email))
        {
            ErrorMessage = "Geçerli bir e-posta adresi girin.";
            return;
        }
        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Şifre girin.";
            return;
        }

        IsBusy = true;
        try
        {
            var user = await _session.LoginAsync(Email.Trim(), Password, RememberMe);

            if (!user.EmailVerified)
            {
                _navigation.Navigate(AppPage.VerifyEmail);
            }
            else
            {
                _navigation.Navigate(AppPage.Home);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = DLI.Connect.Utilities.AppErrors.ToMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToRegister() => _navigation.Navigate(AppPage.Register);

    [RelayCommand]
    private void GoToForgotPassword() => _navigation.Navigate(AppPage.ForgotPassword);
}

