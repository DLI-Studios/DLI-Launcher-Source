using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.ViewModels;

public partial class FriendRequestsViewModel : ViewModelBase
{
    private readonly IFriendService _friends;
    private readonly DispatcherTimer _timer;

    public ObservableCollection<IncomingRequestItemViewModel> Requests { get; } = new();

    [ObservableProperty]
    private string? _errorMessage;

    private bool _refreshing;

    public bool IsEmpty => Requests.Count == 0;
    public string CountText => Requests.Count == 0 ? "Bekleyen istek yok" : $"{Requests.Count} bekleyen istek";

    public FriendRequestsViewModel(IFriendService friends)
    {
        _friends = friends;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(8)
        };
        _timer.Tick += async (_, _) => await RefreshAsync(quiet: true);
    }

    public override void OnNavigatedTo()
    {
        ErrorMessage = null;
        _ = RefreshAsync(quiet: false);
        _timer.Start();
    }

    public override void OnNavigatedFrom()
    {
        _timer.Stop();
    }

    private async Task RefreshAsync(bool quiet)
    {
        if (_refreshing) return;
        _refreshing = true;

        try
        {
            var incoming = await _friends.GetIncomingRequestsAsync();
            var profiles = new List<(FriendRequest Request, UserProfile? Profile)>();

            foreach (var request in incoming)
            {
                var profile = await GetProfileAsync(request.FromUid);
                profiles.Add((request, profile));
            }

            Requests.Clear();
            foreach (var (request, profile) in profiles)
            {
                Requests.Add(new IncomingRequestItemViewModel(
                    request,
                    profile,
                    AcceptAsync,
                    DeclineAsync));
            }

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(CountText));
        }
        catch (Exception ex)
        {
            DLI.Connect.Utilities.AppErrors.Log("FriendRequestsViewModel", ex);
            if (!quiet)
            {
                ErrorMessage = DLI.Connect.Utilities.AppErrors.ToMessage(ex);
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private async Task<UserProfile?> GetProfileAsync(string uid)
    {
        try
        {
            return await _friends.GetProfileAsync(uid);
        }
        catch
        {
            return null;
        }
    }

    private async Task AcceptAsync(IncomingRequestItemViewModel item)
    {
        try
        {
            await _friends.AcceptRequestAsync(item.RequestId, item.FromUid);
            await RefreshAsync(quiet: false);
        }
        catch (Exception ex)
        {
            ErrorMessage = DLI.Connect.Utilities.AppErrors.ToMessage(ex);
        }
    }

    private async Task DeclineAsync(IncomingRequestItemViewModel item)
    {
        try
        {
            await _friends.DeclineRequestAsync(item.RequestId);
            await RefreshAsync(quiet: false);
        }
        catch (Exception ex)
        {
            ErrorMessage = DLI.Connect.Utilities.AppErrors.ToMessage(ex);
        }
    }
}

public partial class IncomingRequestItemViewModel : ObservableObject
{
    private readonly Func<IncomingRequestItemViewModel, Task> _accept;
    private readonly Func<IncomingRequestItemViewModel, Task> _decline;

    public string RequestId { get; }
    public string FromUid { get; }
    public string DisplayName { get; }
    public string Username { get; }
    public string Initial { get; }
    public bool IsOnline { get; }

    [ObservableProperty]
    private bool _isBusy;

    public IncomingRequestItemViewModel(
        FriendRequest request,
        UserProfile? profile,
        Func<IncomingRequestItemViewModel, Task> accept,
        Func<IncomingRequestItemViewModel, Task> decline)
    {
        RequestId = request.RequestId;
        FromUid = request.FromUid;
        DisplayName = profile?.DisplayName ?? request.FromUid[..Math.Min(8, request.FromUid.Length)];
        Username = profile == null ? "" : $"@{profile.Username}";
        Initial = string.IsNullOrWhiteSpace(DisplayName) ? "?" : DisplayName.Trim()[..1].ToUpperInvariant();
        IsOnline = profile?.Status == "online";
        _accept = accept;
        _decline = decline;
    }

    [RelayCommand]
    private async Task AcceptAsync()
    {
        IsBusy = true;
        try
        {
            await _accept(this);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeclineAsync()
    {
        IsBusy = true;
        try
        {
            await _decline(this);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
