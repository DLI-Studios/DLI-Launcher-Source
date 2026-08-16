using System;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;
using DLI.Connect.Utilities;

namespace DLI.Connect.ViewModels;

public partial class ProfileViewModel : ViewModelBase
{
    private readonly ISessionManager _session;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _bio = "";

    [ObservableProperty]
    private bool _isEditingName;

    [ObservableProperty]
    private bool _isEditingBio;

    [ObservableProperty]
    private ImageSource? _avatarPreview;

    [ObservableProperty]
    private byte[]? _pendingAvatarBytes;

    [ObservableProperty]
    private string _status = Presence.Online;

    public string Username => _session.Profile?.Username ?? "";
    public string Email => _session.CurrentUser?.Email ?? "";
    public string Initial => string.IsNullOrWhiteSpace(DisplayName) ? "?" : DisplayName.Trim()[..1].ToUpperInvariant();
    public string AvatarUrl => _session.Profile?.Avatar ?? "";
    public string JoinedDateText => _session.Profile is { CreatedAt: > 0 } p
        ? DateTimeOffset.FromUnixTimeMilliseconds(p.CreatedAt).LocalDateTime.ToString("dd MMMM yyyy")
        : "-";
    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
    public bool HasPendingAvatar => PendingAvatarBytes != null;
    public bool IsOnline => _session.Profile?.IsPresent ?? false;

    public string StatusText => Status switch
    {
        Presence.Away => "Boşta",
        Presence.DoNotDisturb => "Rahatsız Etmeyin",
        Presence.Invisible => "Gizli",
        _ => "Çevrimiçi"
    };

    public string StatusColorHex => Status switch
    {
        Presence.DoNotDisturb => "#F23F43",
        Presence.Away => "#F0B232",
        Presence.Invisible => "#80848E",
        _ => "#23A55A"
    };

    public ProfileViewModel(ISessionManager session)
    {
        _session = session;
    }

    public override void OnNavigatedTo()
    {
        _session.StateChanged += OnSessionChanged;
        LoadFromSession();
    }

    public override void OnNavigatedFrom()
    {
        _session.StateChanged -= OnSessionChanged;
    }

    private void OnSessionChanged()
    {
        LoadFromSession();
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Username));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(Initial));
        OnPropertyChanged(nameof(AvatarUrl));
        OnPropertyChanged(nameof(HasAvatar));
        OnPropertyChanged(nameof(JoinedDateText));
        OnPropertyChanged(nameof(IsOnline));
    }

    private void LoadFromSession()
    {
        DisplayName = _session.Profile?.DisplayName ?? "";
        Bio = _session.Profile?.Bio ?? "";
        Status = _session.Profile?.Status ?? Presence.Online;
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusColorHex));
    }

    partial void OnDisplayNameChanged(string value) => OnPropertyChanged(nameof(Initial));

    [RelayCommand]
    private void StartEditName() => IsEditingName = true;

    [RelayCommand]
    private void CancelEditName()
    {
        DisplayName = _session.Profile?.DisplayName ?? "";
        IsEditingName = false;
    }

    [RelayCommand]
    private async Task SaveDisplayNameAsync()
    {
        var name = DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Görünen ad boş olamaz.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _session.UpdateProfileAsync(displayName: name);
            IsEditingName = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("ProfileViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StartEditBio() => IsEditingBio = true;

    [RelayCommand]
    private void CancelEditBio()
    {
        Bio = _session.Profile?.Bio ?? "";
        IsEditingBio = false;
    }

    [RelayCommand]
    private async Task SaveBioAsync()
    {
        if (Bio.Length > 200)
        {
            ErrorMessage = "Biyografi en fazla 200 karakter olabilir.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _session.UpdateProfileAsync(bio: Bio.Trim());
            IsEditingBio = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("ProfileViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ApplyAvatarBytes(byte[] imageBytes)
    {
        try
        {
            var (preview, jpeg) = AvatarProcessor.CropToSquare(imageBytes);
            AvatarPreview = preview;
            PendingAvatarBytes = jpeg;
            ErrorMessage = null;
            OnPropertyChanged(nameof(HasPendingAvatar));
        }
        catch (Exception ex)
        {
            ErrorMessage = "Geçerli bir görüntü seçmedin.";
            AppErrors.Log("ProfileViewModel", ex);
        }
    }

    public void ShowAvatarError()
    {
        ErrorMessage = "Avatar dosyası okunamadı.";
    }

    [RelayCommand]
    private void CancelAvatar()
    {
        AvatarPreview = null;
        PendingAvatarBytes = null;
        OnPropertyChanged(nameof(HasPendingAvatar));
    }

    [RelayCommand]
    private async Task SaveAvatarAsync()
    {
        if (PendingAvatarBytes == null) return;

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _session.UpdateAvatarAsync(PendingAvatarBytes);
            AvatarPreview = null;
            PendingAvatarBytes = null;
            OnPropertyChanged(nameof(HasPendingAvatar));
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("ProfileViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveAvatarAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _session.RemoveAvatarAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("ProfileViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SetOnline() => _ = SetStatusAsync(Presence.Online);

    [RelayCommand]
    private void SetAway() => _ = SetStatusAsync(Presence.Away);

    [RelayCommand]
    private void SetDoNotDisturb() => _ = SetStatusAsync(Presence.DoNotDisturb);

    [RelayCommand]
    private void SetInvisible() => _ = SetStatusAsync(Presence.Invisible);

    private async Task SetStatusAsync(string status)
    {
        if (Status == status) return;
        ErrorMessage = null;
        try
        {
            await _session.SetStatusAsync(status);
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("ProfileViewModel", ex);
        }
    }
}
