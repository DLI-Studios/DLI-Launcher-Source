using System;
using System.Windows.Controls;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.Services;

public enum AppPage
{
    Login,
    Register,
    ForgotPassword,
    VerifyEmail,
    Home
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    public Func<UserControl>? HostAccessor { get; set; }

    public event Action<AppPage>? PageChanged;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public void Navigate(AppPage page)
    {
        PageChanged?.Invoke(page);
    }
}
