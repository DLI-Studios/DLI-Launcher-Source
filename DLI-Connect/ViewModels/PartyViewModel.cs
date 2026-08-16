using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;
using DLI.Connect.Utilities;

namespace DLI.Connect.ViewModels;

public partial class PartyViewModel : ViewModelBase
{
    private readonly IPartyService _partyService;
    private readonly IFriendService _friendService;
    private readonly ISessionManager _session;
    private readonly IVoiceChatService _voiceChat;

    [ObservableProperty]
    private Party? _party;

    [ObservableProperty]
    private bool _isInParty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ObservableCollection<PartyInviteViewModel> _pendingInvites = new();

    [ObservableProperty]
    private bool _isInVoiceChannel;

    [ObservableProperty]
    private bool _isVoiceMuted;

    [ObservableProperty]
    private bool _isVoiceDeafened;

    [ObservableProperty]
    private string? _voiceStatusText;

    [ObservableProperty]
    private bool _isInvitePanelOpen;

    [ObservableProperty]
    private string _inviteSearchText = "";

    [ObservableProperty]
    private bool _hasSearched;

    public ObservableCollection<VoiceParticipantState> VoiceParticipants => _voiceChat.Participants;

    public ObservableCollection<InviteSearchResultItemViewModel> InviteResults { get; } = new();

    public PartyViewModel(IPartyService partyService, IFriendService friendService, ISessionManager session, IVoiceChatService voiceChat)
    {
        _partyService = partyService;
        _friendService = friendService;
        _session = session;
        _voiceChat = voiceChat;

        _voiceChat.ParticipantStateChanged += OnParticipantStateChanged;
        _voiceChat.VoiceDisconnected += OnVoiceDisconnected;
        _voiceChat.ErrorOccurred += OnVoiceError;
    }

    private void OnParticipantStateChanged(object? sender, VoiceParticipantState e)
    {
        // Update UI on participant state change
    }

    private void OnVoiceDisconnected(object? sender, EventArgs e)
    {
        IsInVoiceChannel = false;
        IsVoiceMuted = false;
        IsVoiceDeafened = false;
        VoiceStatusText = null;
    }

    private void OnVoiceError(object? sender, string e)
    {
        ErrorMessage = e;
    }

    public override async void OnNavigatedTo()
    {
        try
        {
            var invites = await _partyService.GetPendingInvitesAsync();
            PendingInvites.Clear();
            foreach (var invite in invites)
            {
                PendingInvites.Add(new PartyInviteViewModel(invite, _partyService));
            }
        }
        catch { }
        await RefreshPartyAsync();
    }

    public override void OnNavigatedFrom()
    {
        _session.StateChanged -= OnSessionChanged;
        _voiceChat.ParticipantStateChanged -= OnParticipantStateChanged;
        _voiceChat.VoiceDisconnected -= OnVoiceDisconnected;
        _voiceChat.ErrorOccurred -= OnVoiceError;
    }

    private void OnSessionChanged() => _ = RefreshPartyAsync();

    [RelayCommand]
    private async Task RefreshPartyAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            Party = await _partyService.GetCurrentPartyAsync();
            IsInParty = Party != null;
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreatePartyAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var party = await _partyService.CreatePartyAsync();
            if (party == null)
            {
                ErrorMessage = "Parti oluşturulamadı (zaten bir partideysin).";
            }
            else
            {
                await RefreshPartyAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LeavePartyAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _partyService.LeavePartyAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisbandPartyAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _partyService.DisbandPartyAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task InviteFriendAsync(string friendUid)
    {
        ErrorMessage = null;
        try
        {
            await _partyService.InviteFriendAsync(friendUid);
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
    }

    [RelayCommand]
    private void ToggleInvitePanel()
    {
        IsInvitePanelOpen = !IsInvitePanelOpen;
        if (!IsInvitePanelOpen)
        {
            InviteResults.Clear();
            HasSearched = false;
            InviteSearchText = "";
        }
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SearchInviteAsync()
    {
        if (!IsInvitePanelOpen) return;
        if (string.IsNullOrWhiteSpace(InviteSearchText))
        {
            ErrorMessage = "Davet etmek için bir kullanıcı adı gir.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var exclude = _session.CurrentUser?.Uid ?? "";
            var users = await _friendService.SearchUsersAsync(InviteSearchText, exclude, 20);
            var memberUids = Party?.Members.Select(m => m.Uid) ?? Enumerable.Empty<string>();

            InviteResults.Clear();
            foreach (var user in users.Where(u => !memberUids.Contains(u.Uid)))
            {
                InviteResults.Add(new InviteSearchResultItemViewModel(user, _partyService, message => ErrorMessage = message));
            }

            HasSearched = true;
            if (InviteResults.Count == 0)
            {
                ErrorMessage = "Bu kullanıcı adıyla davet edilecek sonuç bulunamadı.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task KickMemberAsync(string memberUid)
    {
        ErrorMessage = null;
        try
        {
            await _partyService.KickMemberAsync(memberUid);
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
    }

    [RelayCommand]
    private async Task TransferLeadershipAsync(string memberUid)
    {
        ErrorMessage = null;
        try
        {
            await _partyService.TransferLeadershipAsync(memberUid);
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
    }

    [RelayCommand]
    private async Task JoinVoiceChannelAsync()
    {
        if (Party == null) return;

        ErrorMessage = null;
        try
        {
            await _voiceChat.JoinVoiceChannelAsync(Party.PartyId);
            IsInVoiceChannel = true;
            VoiceStatusText = "Bağlandı";
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
    }

    [RelayCommand]
    private async Task LeaveVoiceChannelAsync()
    {
        ErrorMessage = null;
        try
        {
            await _voiceChat.LeaveVoiceChannelAsync();
            IsInVoiceChannel = false;
            IsVoiceMuted = false;
            IsVoiceDeafened = false;
            VoiceStatusText = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
    }

    [RelayCommand]
    private async Task ToggleMuteAsync()
    {
        ErrorMessage = null;
        try
        {
            await _voiceChat.ToggleMuteAsync();
            IsVoiceMuted = _voiceChat.IsMuted;
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
    }

    [RelayCommand]
    private async Task ToggleDeafenAsync()
    {
        ErrorMessage = null;
        try
        {
            await _voiceChat.ToggleDeafenAsync();
            IsVoiceDeafened = _voiceChat.IsDeafened;
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
            AppErrors.Log("PartyViewModel", ex);
        }
    }
}

public partial class PartyInviteViewModel : ObservableObject
{
    private readonly IPartyService _partyService;

    public PartyInvite Invite { get; }

    public string FromDisplayName => Invite.FromUid;
    public string FromUsername => Invite.FromUid;

    public PartyInviteViewModel(PartyInvite invite, IPartyService partyService)
    {
        Invite = invite;
        _partyService = partyService;
    }

    [RelayCommand]
    private async Task AcceptAsync()
    {
        await _partyService.AcceptInviteAsync(Invite.InviteId);
    }

    [RelayCommand]
    private async Task DeclineAsync()
    {
        await _partyService.DeclineInviteAsync(Invite.InviteId);
    }
}

public partial class InviteSearchResultItemViewModel : ObservableObject
{
    private readonly IPartyService _partyService;
    private readonly Action<string> _onError;

    public UserProfile Profile { get; }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _inviteSent;

    public string DisplayName => Profile.DisplayName;
    public string Username => $"@{Profile.Username}";
    public string Initial => string.IsNullOrWhiteSpace(Profile.DisplayName) ? "?" : Profile.DisplayName.Trim()[..1].ToUpperInvariant();
    public bool IsOnline => Profile.Privacy.ShowStatus && Profile.IsPresent;

    public bool CanInvite => !IsBusy && !InviteSent;

    public string InviteButtonText => InviteSent ? "Davet Gönderildi" : "Davet Et";

    public InviteSearchResultItemViewModel(UserProfile profile, IPartyService partyService, Action<string> onError)
    {
        Profile = profile;
        _partyService = partyService;
        _onError = onError;
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInvite));
    }

    partial void OnInviteSentChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInvite));
        OnPropertyChanged(nameof(InviteButtonText));
    }

    [RelayCommand]
    private async Task InviteAsync()
    {
        IsBusy = true;
        try
        {
            await _partyService.InviteFriendAsync(Profile.Uid);
            InviteSent = true;
        }
        catch (Exception ex)
        {
            _onError(AppErrors.ToMessage(ex));
        }
        finally
        {
            IsBusy = false;
        }
    }
}