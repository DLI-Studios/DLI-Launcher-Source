using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Services;
using DLI.Connect.Services.Interfaces;
using DLI.Connect.Utilities;

namespace DLI.Connect.ViewModels;

public partial class RegisterViewModel : ViewModelBase
{
    private readonly ISessionManager _session;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _confirmPassword = "";

    [ObservableProperty]
    private string? _errorMessage;

    public RegisterViewModel(ISessionManager session, INavigationService navigation)
    {
        _session = session;
        _navigation = navigation;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ErrorMessage = null;

        if (!Validators.IsValidUsername(Username))
        {
            ErrorMessage = "Kullanıcı adı 3-20 karakter olmalı; harf, rakam ve alt çizgi içerebilir.";
            return;
        }
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ErrorMessage = "Görünen ad girin.";
            return;
        }
        if (!Validators.IsValidEmail(Email))
        {
            ErrorMessage = "Geçerli bir e-posta adresi girin.";
            return;
        }
        if (!Validators.IsValidPassword(Password))
        {
            ErrorMessage = "Şifre en az 6 karakter olmalı.";
            return;
        }
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Şifreler eşleşmiyor.";
            return;
        }

        IsBusy = true;
        try
        {
            await _session.RegisterAsync(Username.Trim(), DisplayName.Trim(), Email.Trim(), Password);
            _navigation.Navigate(AppPage.VerifyEmail);
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
    private void GoToLogin() => _navigation.Navigate(AppPage.Login);
}

