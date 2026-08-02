using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.ViewModels;

public partial class AddFriendsViewModel : ViewModelBase
{
    private readonly IFriendService _friends;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasSearched;

    public ObservableCollection<SearchResultItemViewModel> Results { get; } = new();

    public AddFriendsViewModel(IFriendService friends)
    {
        _friends = friends;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ErrorMessage = "Aramak için bir kullanıcı adı gir.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var users = await _friends.SearchUsersAsync(SearchText, "", 20);

            Results.Clear();
            foreach (var user in users)
            {
                var state = await _friends.GetRelationStateAsync(user.Uid);
                Results.Add(new SearchResultItemViewModel(_friends, user, state, message => ErrorMessage = message));
            }

            HasSearched = true;
            if (Results.Count == 0)
            {
                ErrorMessage = "Bu kullanıcı adıyla sonuç bulunamadı.";
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
}

public partial class SearchResultItemViewModel : ObservableObject
{
    private readonly IFriendService _friends;
    private readonly Action<string> _onError;

    public UserProfile Profile { get; }

    [ObservableProperty]
    private RequestRelationState _state;

    [ObservableProperty]
    private bool _isBusy;

    public string DisplayName => Profile.DisplayName;
    public string Username => $"@{Profile.Username}";
    public string Initial => string.IsNullOrWhiteSpace(Profile.DisplayName) ? "?" : Profile.DisplayName.Trim()[..1].ToUpperInvariant();
    public bool IsOnline => Profile.Privacy.ShowStatus && Profile.IsPresent;

    public bool CanAdd => State == RequestRelationState.None && !IsBusy;

    public string StateText => State switch
    {
        RequestRelationState.AlreadyFriends => "Arkadaşınız",
        RequestRelationState.RequestSent => "İstek Gönderildi",
        RequestRelationState.IncomingRequest => "İstek Aldınız",
        _ => "Arkadaş Ekle"
    };

    public SearchResultItemViewModel(IFriendService friends, UserProfile profile, RequestRelationState state, Action<string> onError)
    {
        _friends = friends;
        Profile = profile;
        _state = state;
        _onError = onError;
    }

    partial void OnStateChanged(RequestRelationState value)
    {
        OnPropertyChanged(nameof(CanAdd));
        OnPropertyChanged(nameof(StateText));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAdd));
    }

    [RelayCommand]
    private async Task AddFriendAsync()
    {
        IsBusy = true;
        try
        {
            await _friends.SendFriendRequestAsync(Profile.Uid);
            State = RequestRelationState.RequestSent;
        }
        catch (Exception ex)
        {
            _onError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
