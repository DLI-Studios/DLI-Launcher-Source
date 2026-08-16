using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;
using DLI.Connect.ViewModels;

namespace DLI.Connect;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ISessionManager _session;
    private readonly IVoiceChatService _voiceChat;
    private bool _closing;

    public MainWindow(MainWindowViewModel viewModel, ISessionManager session, IVoiceChatService voiceChat)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _session = session;
        _voiceChat = voiceChat;
        DataContext = viewModel;

        MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
    }

    private static string PressedKey(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        return key.ToString();
    }

    private bool IsPushToTalkKey(KeyEventArgs e)
    {
        if (_voiceChat.Settings.ActivationMode != VoiceActivationMode.PushToTalk) return false;
        var pttKey = _voiceChat.Settings.PushToTalkKey;
        return !string.IsNullOrEmpty(pttKey) && string.Equals(PressedKey(e), pttKey, System.StringComparison.Ordinal);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsPushToTalkKey(e))
        {
            _voiceChat.SetPushToTalkState(true);
        }
    }

    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (IsPushToTalkKey(e))
        {
            _voiceChat.SetPushToTalkState(false);
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        _voiceChat.SetPushToTalkState(false);
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            _viewModel.MaximizeRestoreCommand.Execute(this);
            return;
        }
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (_closing) return;
        _closing = true;

        if (_session.IsLoggedIn)
        {
            await _session.ShutdownAsync();
        }
    }
}
