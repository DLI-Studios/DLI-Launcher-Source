using System;
using System.Windows;
using System.Windows.Threading;
using DLI.Connect.Firebase;
using DLI.Connect.Services;
using DLI.Connect.Services.Interfaces;
using DLI.Connect.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DLI.Connect;

public partial class App : Application
{
    private IServiceProvider? _services;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            Log(args.Exception);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log(args.ExceptionObject as Exception);
        };

        ShutdownMode = ShutdownMode.OnMainWindowClose;

        _services = ConfigureServices();
        _mainWindow = _services.GetRequiredService<MainWindow>();

        var session = _services.GetRequiredService<ISessionManager>();
        var navigation = _services.GetRequiredService<INavigationService>();
        var theme = _services.GetRequiredService<IThemeManager>();

        MainWindow = _mainWindow;

        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () =>
        {
            var restored = await session.TryRestoreSessionAsync();

            if (restored && !string.IsNullOrWhiteSpace(session.Profile?.Theme))
            {
                theme.Apply(session.Profile.Theme);
            }
            else
            {
                theme.Apply("system");
            }

            navigation.Navigate(restored ? AppPage.Home : AppPage.Login);
            _mainWindow.Show();
        }));
    }


    private static void Log(Exception? ex)
    {
        try
        {
            if (ex == null) return;
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dli-connect.log");
            System.IO.File.AppendAllText(path,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
        }
        catch { }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IFirebaseClient, FirebaseClient>();
        services.AddSingleton<IFirebaseAuth, FirebaseAuth>();
        services.AddSingleton<IFirebaseFirestore, FirebaseFirestore>();
        services.AddSingleton<IFirebaseStorage, FirebaseStorage>();
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<IFriendService, FriendService>();
        services.AddSingleton<IMessagingService, MessagingService>();
        services.AddSingleton<IPartyService, PartyService>();
        services.AddSingleton<IAudioDeviceService, AudioDeviceService>();
        services.AddSingleton<IVoiceChatService, VoiceChatService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeManager, ThemeManager>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<ForgotPasswordViewModel>();
        services.AddTransient<VerifyEmailViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<FriendsViewModel>();
        services.AddTransient<AddFriendsViewModel>();
        services.AddTransient<FriendRequestsViewModel>();
        services.AddTransient<MessagesViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<PartyViewModel>();

        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<MainWindow>();

        services.AddSingleton<Func<LoginViewModel>>(sp => () => sp.GetRequiredService<LoginViewModel>());
        services.AddSingleton<Func<RegisterViewModel>>(sp => () => sp.GetRequiredService<RegisterViewModel>());
        services.AddSingleton<Func<ForgotPasswordViewModel>>(sp => () => sp.GetRequiredService<ForgotPasswordViewModel>());
        services.AddSingleton<Func<VerifyEmailViewModel>>(sp => () => sp.GetRequiredService<VerifyEmailViewModel>());
        services.AddSingleton<Func<HomeViewModel>>(sp => () => sp.GetRequiredService<HomeViewModel>());
        services.AddSingleton<Func<FriendsViewModel>>(sp => () => sp.GetRequiredService<FriendsViewModel>());
        services.AddSingleton<Func<AddFriendsViewModel>>(sp => () => sp.GetRequiredService<AddFriendsViewModel>());
        services.AddSingleton<Func<FriendRequestsViewModel>>(sp => () => sp.GetRequiredService<FriendRequestsViewModel>());
        services.AddSingleton<Func<MessagesViewModel>>(sp => () => sp.GetRequiredService<MessagesViewModel>());
        services.AddSingleton<Func<ProfileViewModel>>(sp => () => sp.GetRequiredService<ProfileViewModel>());
        services.AddSingleton<Func<SettingsViewModel>>(sp => () => sp.GetRequiredService<SettingsViewModel>());
        services.AddSingleton<Func<PartyViewModel>>(sp => () => sp.GetRequiredService<PartyViewModel>());

        return services.BuildServiceProvider();
    }
}
