using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Services;
using DLI.Connect.Services.Interfaces;
using DLI.Connect.Utilities;

namespace DLI.Connect.ViewModels;

public partial class ForgotPasswordViewModel : ViewModelBase
{
    private readonly ISessionManager _session;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _emailSent;

    public ForgotPasswordViewModel(ISessionManager session, INavigationService navigation)
    {
        _session = session;
        _navigation = navigation;
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        ErrorMessage = null;
        EmailSent = false;

        if (!Validators.IsValidEmail(Email))
        {
            ErrorMessage = "Geçerli bir e-posta adresi girin.";
            return;
        }

        IsBusy = true;
        try
        {
            await _session.ForgotPasswordAsync(Email.Trim());
            EmailSent = true;
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
    private void GoBack() => _navigation.Navigate(AppPage.Login);
}

