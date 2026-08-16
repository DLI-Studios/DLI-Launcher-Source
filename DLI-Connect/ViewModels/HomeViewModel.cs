using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Models;
using DLI.Connect.Services;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.ViewModels;

public enum HomeSection
{
    Overview,
    Friends,
    AddFriends,
    Requests,
    Messages,
    Profile,
    Settings,
    Party
}

public partial class HomeViewModel : ViewModelBase
{
    private readonly ISessionManager _session;
    private readonly INavigationService _navigation;
    private readonly Dictionary<HomeSection, ViewModelBase> _sections = new();
    private readonly Dictionary<HomeSection, Func<ViewModelBase>> _factories;

    [ObservableProperty]
    private HomeSection _section = HomeSection.Overview;

    public ViewModelBase? CurrentSection { get; private set; }

    public UserProfile? Profile => _session.Profile;
    public string Email => _session.CurrentUser?.Email ?? "";
    private string FallbackName =>
        Email.Contains('@') ? Email[..Email.IndexOf('@')] : "Kullanıcı";
    public string DisplayName => Profile?.DisplayName ?? FallbackName;
    public string Username => $"@{Profile?.Username ?? FallbackName}";
    public string Initial => string.IsNullOrWhiteSpace(DisplayName) ? "?" : DisplayName.Trim()[..1].ToUpperInvariant();
    public bool IsOnline => Profile?.IsPresent ?? false;
    public string StatusText => Profile == null ? "Çevrimdışı"
        : Profile.Status switch
        {
            Models.Presence.Away => "Boşta",
            Models.Presence.DoNotDisturb => "Rahatsız Etmeyin",
            Models.Presence.Invisible => "Gizli",
            Models.Presence.Online => "Çevrimiçi",
            _ => "Çevrimdışı"
        };
    public string StatusColorHex => Profile?.Status switch
    {
        Models.Presence.DoNotDisturb => "#F23F43",
        Models.Presence.Away => "#F0B232",
        Models.Presence.Online => "#23A55A",
        _ => "#80848E"
    };
    public bool IsOverview => Section == HomeSection.Overview;

    public HomeViewModel(
        ISessionManager session,
        INavigationService navigation,
        Func<FriendsViewModel> friendsFactory,
        Func<AddFriendsViewModel> addFriendsFactory,
        Func<FriendRequestsViewModel> requestsFactory,
        Func<MessagesViewModel> messagesFactory,
        Func<ProfileViewModel> profileFactory,
        Func<SettingsViewModel> settingsFactory,
        Func<PartyViewModel> partyFactory)
    {
        _session = session;
        _navigation = navigation;

        _factories = new Dictionary<HomeSection, Func<ViewModelBase>>
        {
            [HomeSection.Friends] = friendsFactory,
            [HomeSection.AddFriends] = addFriendsFactory,
            [HomeSection.Requests] = requestsFactory,
            [HomeSection.Messages] = messagesFactory,
            [HomeSection.Profile] = profileFactory,
            [HomeSection.Settings] = settingsFactory,
            [HomeSection.Party] = partyFactory
        };

        _session.StateChanged += OnSessionChanged;
    }

    private void OnSessionChanged()
    {
        OnPropertyChanged(nameof(Profile));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Username));
        OnPropertyChanged(nameof(Initial));
        OnPropertyChanged(nameof(IsOnline));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusColorHex));
    }

    [RelayCommand]
    private void GoOverview() => SetSection(HomeSection.Overview);

    [RelayCommand]
    private void GoFriends() => SetSection(HomeSection.Friends);

    [RelayCommand]
    private void GoAddFriends() => SetSection(HomeSection.AddFriends);

    [RelayCommand]
    private void GoRequests() => SetSection(HomeSection.Requests);

    [RelayCommand]
    private void GoMessages() => SetSection(HomeSection.Messages);

    [RelayCommand]
    private void GoProfile() => SetSection(HomeSection.Profile);

    [RelayCommand]
    private void GoSettings() => SetSection(HomeSection.Settings);

    [RelayCommand]
    private void GoParty() => SetSection(HomeSection.Party);

    private void SetSection(HomeSection section)
    {
        if (Section == section && CurrentSection != null)
        {
            return;
        }

        if (section == HomeSection.Overview)
        {
            CurrentSection?.OnNavigatedFrom();
            CurrentSection = null;
            Section = section;
            OnPropertyChanged(nameof(Section));
            OnPropertyChanged(nameof(IsOverview));
            OnPropertyChanged(nameof(CurrentSection));
            return;
        }

        if (!_sections.TryGetValue(section, out var vm))
        {
            vm = _factories[section]();
            _sections[section] = vm;
        }

        CurrentSection?.OnNavigatedFrom();
        CurrentSection = vm;
        vm.OnNavigatedTo();

        Section = section;
        OnPropertyChanged(nameof(Section));
        OnPropertyChanged(nameof(IsOverview));
        OnPropertyChanged(nameof(CurrentSection));
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
}
