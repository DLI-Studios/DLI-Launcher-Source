using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Services;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.ViewModels;

public partial class VerifyEmailViewModel : ViewModelBase
{
    private readonly ISessionManager _session;
    private readonly INavigationService _navigation;

    private CancellationTokenSource? _pollCts;

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _verificationSent;

    public VerifyEmailViewModel(ISessionManager session, INavigationService navigation)
    {
        _session = session;
        _navigation = navigation;
    }

    public override void OnNavigatedTo()
    {
        Email = _session.CurrentUser?.Email ?? "";
        VerificationSent = false;
        ErrorMessage = null;

        _pollCts = new CancellationTokenSource();
        _ = PollVerificationAsync(_pollCts.Token);
    }

    public override void OnNavigatedFrom()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
    }

    private async Task PollVerificationAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var user = await _session.RefreshVerificationStateAsync();
                if (user.EmailVerified)
                {
                    _navigation.Navigate(AppPage.Home);
                    return;
                }
            }
            catch
            {
                // Retry on next poll
            }
            await Task.Delay(3000, token);
        }
    }

    [RelayCommand]
    private async Task ResendAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _session.ResendVerificationAsync();
            VerificationSent = true;
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
    private async Task CheckNowAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var user = await _session.RefreshVerificationStateAsync();
            if (user.EmailVerified)
            {
                _navigation.Navigate(AppPage.Home);
            }
            else
            {
                ErrorMessage = "E-posta henüz doğrulanmadı.";
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
    private async Task LogoutAsync()
    {
        await _session.LogoutAsync();
        _navigation.Navigate(AppPage.Login);
    }
}

