using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Services;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly Func<LoginViewModel> _loginFactory;
    private readonly Func<RegisterViewModel> _registerFactory;
    private readonly Func<ForgotPasswordViewModel> _forgotFactory;
    private readonly Func<VerifyEmailViewModel> _verifyFactory;
    private readonly Func<HomeViewModel> _homeFactory;

    public ViewModelBase CurrentViewModel { get; private set; }

    public MainWindowViewModel(
        INavigationService navigation,
        Func<LoginViewModel> loginFactory,
        Func<RegisterViewModel> registerFactory,
        Func<ForgotPasswordViewModel> forgotFactory,
        Func<VerifyEmailViewModel> verifyFactory,
        Func<HomeViewModel> homeFactory)
    {
        _navigation = navigation;
        _loginFactory = loginFactory;
        _registerFactory = registerFactory;
        _forgotFactory = forgotFactory;
        _verifyFactory = verifyFactory;
        _homeFactory = homeFactory;

        CurrentViewModel = _loginFactory();

        _navigation.PageChanged += OnPageChanged;
    }

    private void OnPageChanged(AppPage page)
    {
        ViewModelBase? vm = page switch
        {
            AppPage.Login => _loginFactory(),
            AppPage.Register => _registerFactory(),
            AppPage.ForgotPassword => _forgotFactory(),
            AppPage.VerifyEmail => _verifyFactory(),
            AppPage.Home => _homeFactory(),
            _ => null
        };

        if (vm == null) return;

        CurrentViewModel.OnNavigatedFrom();
        CurrentViewModel = vm;
        CurrentViewModel.OnNavigatedTo();
        OnPropertyChanged(nameof(CurrentViewModel));
    }

    [RelayCommand]
    private void Minimize(Window window)
    {
        if (window != null) window.WindowState = WindowState.Minimized;
    }

    [RelayCommand]
    private void MaximizeRestore(Window window)
    {
        if (window == null) return;
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    [RelayCommand]
    private void Close(Window window)
    {
        window?.Close();
    }
}