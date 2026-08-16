using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.ViewModels;

public partial class FriendsViewModel : ViewModelBase
{
    private readonly IFriendService _friends;
    private readonly DispatcherTimer _timer;

    public ObservableCollection<FriendInfo> Friends { get; } = new();

    [ObservableProperty]
    private FriendInfo? _pendingRemove;

    [ObservableProperty]
    private string? _errorMessage;

    private bool _refreshing;

    public bool IsEmpty => Friends.Count == 0;
    public string CountText => Friends.Count == 0 ? "Henüz arkadaşın yok" : $"{Friends.Count} arkadaş";
    public bool IsRemoveDialogOpen => PendingRemove != null;
    public string PendingRemoveDisplayText => PendingRemove == null
        ? ""
        : $"\"{PendingRemove.DisplayName}\" arkadaşlıktan çıkarılacak. Bu işlem geri alınamaz.";

    public FriendsViewModel(IFriendService friends)
    {
        _friends = friends;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
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
            var list = await _friends.GetFriendsAsync();

            var changed = Friends.Count != list.Count ||
                          Friends.Zip(list, (a, b) => a.IsOnline == b.IsOnline && a.Uid == b.Uid)
                              .Any(match => !match);

            if (changed)
            {
                Friends.Clear();
                foreach (var friend in list)
                {
                    Friends.Add(friend);
                }
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
        catch (Exception ex)
        {
            DLI.Connect.Utilities.AppErrors.Log("FriendsViewModel", ex);
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

    [RelayCommand]
    private void CancelRemove()
    {
        PendingRemove = null;
        OnPropertyChanged(nameof(IsRemoveDialogOpen));
        OnPropertyChanged(nameof(PendingRemoveDisplayText));
    }
}
