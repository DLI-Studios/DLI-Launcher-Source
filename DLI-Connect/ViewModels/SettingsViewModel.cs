using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Models;
using DLI.Connect.Services;
using DLI.Connect.Services.Interfaces;
using DLI.Connect.Utilities;

namespace DLI.Connect.ViewModels;

public enum SettingsCategory
{
    General,
    Account,
    Appearance,
    Notifications,
    Privacy,
    Audio,
    Advanced
}

public class SettingsCategoryInfo
{
    public SettingsCategory Category { get; init; }
    public string Title { get; init; } = "";
    public string Icon { get; init; } = "";
    public string Description { get; init; } = "";
    public List<string> Keywords { get; init; } = new();
}

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISessionManager _session;
    private readonly IThemeManager _theme;
    private readonly INavigationService _navigation;
    private readonly IAudioDeviceService _audioDeviceService;
    private readonly IVoiceChatService _voiceChatService;

    public IReadOnlyList<SettingsCategoryInfo> AllCategories { get; }

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private SettingsCategory _selectedCategory = SettingsCategory.General;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private string _themeDark = "dark";

    [ObservableProperty]
    private bool _privacyShowStatus = true;

    [ObservableProperty]
    private bool _privacyShowActivity = true;

    [ObservableProperty]
    private string _privacyFriendRequests = "everyone";

    [ObservableProperty]
    private bool _notifEnabled = true;

    [ObservableProperty]
    private bool _notifFriendRequests = true;

    [ObservableProperty]
    private bool _notifMessages = true;

    [ObservableProperty]
    private bool _notifPartyInvites = true;

    [ObservableProperty]
    private string _currentPassword = "";

    [ObservableProperty]
    private string _newPassword = "";

    [ObservableProperty]
    private string _confirmPassword = "";

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    // Audio settings
    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<string> _inputDevices = new();

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<string> _outputDevices = new();

    [ObservableProperty]
    private string _selectedInputDevice = "";

    [ObservableProperty]
    private string _selectedOutputDevice = "";

    [ObservableProperty]
    private float _inputVolume = 1.0f;

    [ObservableProperty]
    private float _outputVolume = 1.0f;

    [ObservableProperty]
    private float _voiceActivityThreshold = 0.4f;

    [ObservableProperty]
    private string _pushToTalkKey = "";

    [ObservableProperty]
    private bool _noiseSuppression = true;

    [ObservableProperty]
    private bool _echoCancellation = true;

    [ObservableProperty]
    private bool _automaticGainControl = true;

    [ObservableProperty]
    private bool _usePushToTalk;

    public SettingsCategoryInfo? SelectedInfo => AllCategories.FirstOrDefault(c => c.Category == SelectedCategory);
    public string SearchEmptyText => string.IsNullOrWhiteSpace(SearchText)
        ? "Ayarlarda ara..."
        : "Aramanla eşleşen ayar bulunamadı.";

    public bool IsGeneral => SelectedCategory == SettingsCategory.General;
    public bool IsAccount => SelectedCategory == SettingsCategory.Account;
    public bool IsAppearance => SelectedCategory == SettingsCategory.Appearance;
    public bool IsNotifications => SelectedCategory == SettingsCategory.Notifications;
    public bool IsPrivacy => SelectedCategory == SettingsCategory.Privacy;
    public bool IsAudio => SelectedCategory == SettingsCategory.Audio;
    public bool IsAdvanced => SelectedCategory == SettingsCategory.Advanced;

    public string DisplayName => _session.Profile?.DisplayName ?? "";
    public string Username => _session.Profile?.Username ?? "";
    public string Email => _session.CurrentUser?.Email ?? "";
    public string Initial => string.IsNullOrWhiteSpace(DisplayName) ? "?" : DisplayName.Trim()[..1].ToUpperInvariant();
    public string AvatarUrl => _session.Profile?.Avatar ?? "";
    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
    public string ThemeText => _theme.CurrentTheme switch
    {
        "light" => "Açık",
        "system" => "Sistem",
        _ => "Koyu"
    };

    public SettingsViewModel(
        ISessionManager session,
        IThemeManager theme,
        INavigationService navigation,
        IAudioDeviceService audioDeviceService,
        IVoiceChatService voiceChatService)
    {
        _session = session;
        _theme = theme;
        _navigation = navigation;
        _audioDeviceService = audioDeviceService;
        _voiceChatService = voiceChatService;
        AllCategories = new List<SettingsCategoryInfo>
        {
            new() { Category = SettingsCategory.General, Title = "Genel", Icon = "\uE713", Description = "Hesap ve profil özeti.", Keywords = new() { "genel", "profil", "hesap", "özet" } },
            new() { Category = SettingsCategory.Account, Title = "Hesap", Icon = "\uE77B", Description = "Şifre değiştir, oturum yönet.", Keywords = new() { "hesap", "şifre", "çıkış", "sil", "parola" } },
            new() { Category = SettingsCategory.Appearance, Title = "Görünüm", Icon = "\uE790", Description = "Tema ve görünüm seçenekleri.", Keywords = new() { "görünüm", "tema", "koyu", "açık", "sistem", "dark", "light" } },
            new() { Category = SettingsCategory.Notifications, Title = "Bildirimler", Icon = "\uEA8F", Description = "Bildirim tercihlerini yönet.", Keywords = new() { "bildirim", "mesaj", "arkadaşlık", "davet" } },
            new() { Category = SettingsCategory.Privacy, Title = "Gizlilik", Icon = "\uE72E", Description = "Gizlilik tercihlerini yönet.", Keywords = new() { "gizlilik", "durum", "çevrimiçi", "etkinlik", "arkadaşlık isteği" } },
            new() { Category = SettingsCategory.Audio, Title = "Ses", Icon = "\uE767", Description = "Ses ayarları (yakında).", Keywords = new() { "ses", "mikrofon", "hoparlör", "audio", "voice" } },
            new() { Category = SettingsCategory.Advanced, Title = "Gelişmiş", Icon = "\uE712", Description = "Gelişmiş seçenekler (yakında).", Keywords = new() { "gelişmiş", "advanced", "günlük", "cache" } }
        };
    }

    public override void OnNavigatedTo()
    {
        _session.StateChanged += OnSessionChanged;
        _theme.ThemeChanged += OnThemeChanged;
        LoadSettings();
        SuccessMessage = null;
        ErrorMessage = null;
        _ = RefreshAudioDevicesAsync();
    }

    public override void OnNavigatedFrom()
    {
        _session.StateChanged -= OnSessionChanged;
        _theme.ThemeChanged -= OnThemeChanged;
    }

    private void OnSessionChanged() => OnPropertyChanged(nameof(DisplayName));

    private void OnThemeChanged()
    {
        OnPropertyChanged(nameof(ThemeText));
    }

    private void LoadSettings()
    {
        var p = _session.Profile;
        if (p == null) return;

        PrivacyShowStatus = p.Privacy.ShowStatus;
        PrivacyShowActivity = p.Privacy.ShowActivity;
        PrivacyFriendRequests = p.Privacy.FriendRequests;
        NotifEnabled = p.Notifications.Enabled;
        NotifFriendRequests = p.Notifications.FriendRequests;
        NotifMessages = p.Notifications.Messages;
        NotifPartyInvites = p.Notifications.PartyInvites;

        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Username));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(Initial));
        OnPropertyChanged(nameof(AvatarUrl));
        OnPropertyChanged(nameof(HasAvatar));
        OnPropertyChanged(nameof(ThemeText));
    }

    partial void OnSearchTextChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var q = value.Trim().ToLowerInvariant();
            var match = AllCategories.FirstOrDefault(c =>
                c.Title.ToLowerInvariant().Contains(q) ||
                c.Keywords.Any(k => k.ToLowerInvariant().Contains(q)));

            if (match != null)
            {
                SelectedCategory = match.Category;
            }
        }
        OnPropertyChanged(nameof(IsGeneral));
        OnPropertyChanged(nameof(IsAccount));
        OnPropertyChanged(nameof(IsAppearance));
        OnPropertyChanged(nameof(IsNotifications));
        OnPropertyChanged(nameof(IsPrivacy));
        OnPropertyChanged(nameof(IsAudio));
        OnPropertyChanged(nameof(IsAdvanced));
    }

    [RelayCommand]
    private void SelectCategory(SettingsCategory category)
    {
        SelectedCategory = category;
        SuccessMessage = null;
        ErrorMessage = null;
        OnPropertyChanged(nameof(IsGeneral));
        OnPropertyChanged(nameof(IsAccount));
        OnPropertyChanged(nameof(IsAppearance));
        OnPropertyChanged(nameof(IsNotifications));
        OnPropertyChanged(nameof(IsPrivacy));
        OnPropertyChanged(nameof(IsAudio));
        OnPropertyChanged(nameof(IsAdvanced));
    }

    // ---- Theme ----

    [RelayCommand]
    private async Task ApplyThemeAsync(string theme)
    {
        try
        {
            _theme.Apply(theme);
            await _session.UpdateSettingsAsync(theme: theme);
            SuccessMessage = "Tema kaydedildi.";
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("SettingsViewModel", ex);
        }
    }

    // ---- Privacy ----

    partial void OnPrivacyShowStatusChanged(bool value) => _ = SavePrivacyAsync();
    partial void OnPrivacyShowActivityChanged(bool value) => _ = SavePrivacyAsync();

    partial void OnPrivacyFriendRequestsChanged(string value) => _ = SavePrivacyAsync();

    private async Task SavePrivacyAsync()
    {
        try
        {
            await _session.UpdateSettingsAsync(privacy: new UserPrivacy
            {
                FriendRequests = PrivacyFriendRequests,
                ShowStatus = PrivacyShowStatus,
                ShowActivity = PrivacyShowActivity
            });
            SuccessMessage = "Gizlilik ayarları kaydedildi.";
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("SettingsViewModel", ex);
        }
    }

    // ---- Notifications ----

    partial void OnNotifEnabledChanged(bool value) => _ = SaveNotificationsAsync();
    partial void OnNotifFriendRequestsChanged(bool value) => _ = SaveNotificationsAsync();
    partial void OnNotifMessagesChanged(bool value) => _ = SaveNotificationsAsync();
    partial void OnNotifPartyInvitesChanged(bool value) => _ = SaveNotificationsAsync();

    private async Task SaveNotificationsAsync()
    {
        try
        {
            await _session.UpdateSettingsAsync(notifications: new UserNotifications
            {
                Enabled = NotifEnabled,
                FriendRequests = NotifFriendRequests,
                Messages = NotifMessages,
                PartyInvites = NotifPartyInvites
            });
            SuccessMessage = "Bildirim ayarları kaydedildi.";
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("SettingsViewModel", ex);
        }
    }

    // ---- Account ----

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            ErrorMessage = "Mevcut şifreni gir.";
            return;
        }
        if (NewPassword.Length < 6)
        {
            ErrorMessage = "Yeni şifre en az 6 karakter olmalı.";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Yeni şifreler eşleşmiyor.";
            return;
        }

        IsBusy = true;
        SuccessMessage = null;
        ErrorMessage = null;
        try
        {
            await _session.ChangePasswordAsync(CurrentPassword, NewPassword);
            CurrentPassword = "";
            NewPassword = "";
            ConfirmPassword = "";
            SuccessMessage = "Şifren değiştirildi.";
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("SettingsViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        IsBusy = true;
        try
        {
            await _session.LogoutAsync();
            _navigation.Navigate(AppPage.Login);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenDeleteConfirm()
    {
        IsDeleteConfirmOpen = true;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void CancelDeleteConfirm() => IsDeleteConfirmOpen = false;

    [RelayCommand]
    private async Task ConfirmDeleteAccountAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _session.DeleteAccountAsync();
            IsDeleteConfirmOpen = false;
            _navigation.Navigate(AppPage.Login);
        }
        catch (Exception ex)
        {
            IsDeleteConfirmOpen = false;
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("SettingsViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---- Audio ----

    [RelayCommand]
    private async Task RefreshAudioDevicesAsync()
    {
        try
        {
            await _audioDeviceService.RefreshDevicesAsync();

            InputDevices.Clear();
            OutputDevices.Clear();

            foreach (var device in _audioDeviceService.AvailableInputDevices)
            {
                InputDevices.Add(device.Name);
            }

            foreach (var device in _audioDeviceService.AvailableOutputDevices)
            {
                OutputDevices.Add(device.Name);
            }

            var currentInput = _audioDeviceService.SelectedInputDeviceId;
            var currentOutput = _audioDeviceService.SelectedOutputDeviceId;

            var inputDevice = _audioDeviceService.AvailableInputDevices.FirstOrDefault(d => d.Id == currentInput);
            if (inputDevice != null)
            {
                SelectedInputDevice = inputDevice.Name;
            }

            var outputDevice = _audioDeviceService.AvailableOutputDevices.FirstOrDefault(d => d.Id == currentOutput);
            if (outputDevice != null)
            {
                SelectedOutputDevice = outputDevice.Name;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("SettingsViewModel", ex);
        }
    }

    partial void OnSelectedInputDeviceChanged(string value)
    {
        var device = _audioDeviceService.AvailableInputDevices.FirstOrDefault(d => d.Name == value);
        if (device != null)
        {
            _ = _voiceChatService.SetInputDeviceAsync(device.Id);
        }
    }

    partial void OnSelectedOutputDeviceChanged(string value)
    {
        var device = _audioDeviceService.AvailableOutputDevices.FirstOrDefault(d => d.Name == value);
        if (device != null)
        {
            _ = _voiceChatService.SetOutputDeviceAsync(device.Id);
        }
    }

    partial void OnInputVolumeChanged(float value) => _ = _voiceChatService.SetInputVolumeAsync(value);

    partial void OnOutputVolumeChanged(float value) => _ = _voiceChatService.SetOutputVolumeAsync(value);

    partial void OnNoiseSuppressionChanged(bool value) => ApplyVoiceSettings();

    partial void OnEchoCancellationChanged(bool value) => ApplyVoiceSettings();

    partial void OnAutomaticGainControlChanged(bool value) => ApplyVoiceSettings();

    partial void OnUsePushToTalkChanged(bool value) => ApplyVoiceSettings();

    partial void OnPushToTalkKeyChanged(string value) => ApplyVoiceSettings();

    partial void OnVoiceActivityThresholdChanged(float value) => ApplyVoiceSettings();

    private void ApplyVoiceSettings()
    {
        var settings = new VoiceSettings
        {
            InputVolume = InputVolume,
            OutputVolume = OutputVolume,
            VoiceActivityThreshold = VoiceActivityThreshold,
            NoiseSuppression = NoiseSuppression,
            EchoCancellation = EchoCancellation,
            AutomaticGainControl = AutomaticGainControl,
            ActivationMode = UsePushToTalk ? VoiceActivationMode.PushToTalk : VoiceActivationMode.VoiceActivity,
            PushToTalkKey = PushToTalkKey
        };

        _ = _voiceChatService.ApplySettingsAsync(settings);
    }

    [RelayCommand]
    private void ResetAudioDefaults()
    {
        InputVolume = 1.0f;
        OutputVolume = 1.0f;
        VoiceActivityThreshold = 0.4f;
        PushToTalkKey = "";
        NoiseSuppression = true;
        EchoCancellation = true;
        AutomaticGainControl = true;
        UsePushToTalk = false;

        ApplyVoiceSettings();
        SuccessMessage = "Ses ayarları varsayılana döndürüldü.";
    }
}
