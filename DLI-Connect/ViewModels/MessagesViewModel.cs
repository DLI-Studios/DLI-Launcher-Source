using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLI.Connect.Models;
using DLI.Connect.Services;
using DLI.Connect.Services.Interfaces;
using DLI.Connect.Utilities;

namespace DLI.Connect.ViewModels;

public partial class MessagesViewModel : ViewModelBase
{
    private readonly IMessagingService _messaging;
    private readonly IFriendService _friends;
    private readonly ISessionManager _session;
    private readonly Dictionary<string, UserProfile> _profiles = new();
    private readonly List<ConversationItemViewModel> _allItems = new();
    private readonly List<MessageItemViewModel> _loadedItems = new();
    private readonly DispatcherTimer _timer;

    private bool _refreshing;
    private bool _isLoadingOlder;
    private bool _allOlderLoaded;
    private bool _windowActive = true;
    private DateTime _lastTypingSent = DateTime.MinValue;
    private string? _loadedConversationId;

    public event Action<bool>? ScrollToBottomRequested;
    public event Action? OlderMessagesLoaded;

    public ObservableCollection<ConversationItemViewModel> Conversations { get; } = new();
    public ObservableCollection<MessageItemViewModel> Messages { get; } = new();

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private ConversationItemViewModel? _selected;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _messageText = "";

    [ObservableProperty]
    private bool _isPeerTyping;

    [ObservableProperty]
    private bool _hasOpened;

    [ObservableProperty]
    private bool _hasMore;

    [ObservableProperty]
    private bool _isNewChatDialogOpen;

    public ObservableCollection<FriendInfo> NewChatFriends { get; } = new();

    public string Me => _session.CurrentUser?.Uid ?? "";

    public MessagesViewModel(IMessagingService messaging, IFriendService friends, ISessionManager session)
    {
        _messaging = messaging;
        _friends = friends;
        _session = session;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += async (_, _) => await TickAsync();
    }

    public override void OnNavigatedTo()
    {
        ErrorMessage = null;
        AttachFocusTracking();
        _ = PollAsync(quiet: false);
        _timer.Start();
    }

    public override void OnNavigatedFrom()
    {
        _timer.Stop();
        DetachFocusTracking();
    }

    private async Task TickAsync()
    {
        var target = _windowActive ? TimeSpan.FromSeconds(2) : TimeSpan.FromSeconds(10);
        if (_timer.Interval != target)
        {
            _timer.Interval = target;
        }
        await PollAsync(quiet: true);
    }

    private void AttachFocusTracking()
    {
        var window = System.Windows.Application.Current?.MainWindow;
        if (window == null) return;
        window.Activated += OnWindowActivated;
        window.Deactivated += OnWindowDeactivated;
        _windowActive = window.IsActive;
    }

    private void DetachFocusTracking()
    {
        var window = System.Windows.Application.Current?.MainWindow;
        if (window == null) return;
        window.Activated -= OnWindowActivated;
        window.Deactivated -= OnWindowDeactivated;
    }

    private void OnWindowActivated(object? sender, EventArgs e) => _windowActive = true;
    private void OnWindowDeactivated(object? sender, EventArgs e) => _windowActive = false;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedChanged(ConversationItemViewModel? value)
    {
        foreach (var item in _allItems)
        {
            item.IsSelected = item == value;
        }
    }

    partial void OnMessageTextChanged(string value)
    {
        var now = DateTime.UtcNow;
        if (Selected != null && !string.IsNullOrWhiteSpace(value) && (now - _lastTypingSent).TotalSeconds > 3)
        {
            _lastTypingSent = now;
            _ = _messaging.SetTypingAsync(Selected.Conversation.ConversationId);
        }
    }

    private async Task PollAsync(bool quiet)
    {
        if (_refreshing) return;
        _refreshing = true;

        try
        {
            await RefreshConversationsAsync();

            if (Selected != null)
            {
                await RefreshChatAsync();
            }
        }
        catch (Exception ex)
        {
            AppErrors.Log("MessagesViewModel", ex);
            if (!quiet)
            {
                ErrorMessage = AppErrors.ToMessage(ex);
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private async Task RefreshConversationsAsync()
    {
        var list = await _messaging.GetConversationsAsync();
        if (string.IsNullOrEmpty(Me)) return;

        var known = _allItems.ToDictionary(i => i.Conversation.ConversationId);

        foreach (var conversation in list)
        {
            if (!known.TryGetValue(conversation.ConversationId, out var item))
            {
                item = new ConversationItemViewModel(conversation, Me);
                _allItems.Add(item);
            }
            item.Update(conversation);
            item.SetProfile(await GetProfileAsync(conversation.PeerUid(Me)));
        }

        foreach (var removed in _allItems.Where(i => !list.Any(c => c.ConversationId == i.Conversation.ConversationId)).ToList())
        {
            _allItems.Remove(removed);
        }

        ApplyFilter();
    }

    private async Task RefreshChatAsync()
    {
        if (Selected == null) return;

        var conversation = Selected.Conversation;
        if (_loadedConversationId != conversation.ConversationId)
        {
            _loadedConversationId = conversation.ConversationId;
            _loadedItems.Clear();
            _allOlderLoaded = false;
            HasMore = false;
            Messages.Clear();
        }

        var fresh = await _messaging.GetMessagesAsync(conversation.ConversationId);
        MergeFresh(fresh);

        await ApplySenderNamesAsync(conversation);

        var peerUnread = _loadedItems.Any(m => !m.IsOwn && !m.Message.Read);
        if (peerUnread)
        {
            var unread = _loadedItems.Where(m => !m.IsOwn && !m.Message.Read).Select(m => m.Message).ToList();
            await _messaging.MarkReadAsync(conversation, unread);
            foreach (var item in _loadedItems.Where(m => !m.IsOwn))
            {
                item.ApplyRead(true);
            }
            var conv = _allItems.FirstOrDefault(i => i.Conversation.ConversationId == conversation.ConversationId);
            conv?.ApplyUnread(0);
        }

        if (!_allOlderLoaded && !HasMore)
        {
            HasMore = fresh.Count >= MessagingService.PageSize;
        }

        IsPeerTyping = await _messaging.IsPeerTypingAsync(conversation);
    }

    private void MergeFresh(List<Message> fresh)
    {
        var changed = false;
        foreach (var message in fresh)
        {
            var existing = _loadedItems.FirstOrDefault(x => x.Message.MessageId == message.MessageId);
            if (existing != null)
            {
                if (existing.UpdateFromServer(message)) changed = true;
            }
            else
            {
                var item = new MessageItemViewModel(message, Me);
                _loadedItems.Add(item);
                changed = true;

                if (!item.IsOwn)
                {
                    ScrollToBottomRequested?.Invoke(false);
                }
            }
        }

        if (changed)
        {
            RebuildMessages();
        }
    }

    private async Task ApplySenderNamesAsync(ConversationInfo conversation)
    {
        var myName = _session.CurrentUser?.DisplayName ?? "Sen";
        var peerName = (await GetProfileAsync(conversation.PeerUid(Me))).DisplayName;
        foreach (var item in Messages)
        {
            item.SenderName = item.IsOwn ? myName : peerName;
        }
    }

    private void RebuildMessages()
    {
        var ordered = _loadedItems.OrderBy(x => x.Message.CreatedAt).ToList();

        DateTime? prevDay = null;
        for (var i = 0; i < ordered.Count; i++)
        {
            var item = ordered[i];
            var day = DateTimeOffset.FromUnixTimeMilliseconds(item.Message.CreatedAt).LocalDateTime.Date;
            item.ApplyDaySeparator(prevDay == null || day != prevDay.Value, DayText(day));
            prevDay = day;

            var prev = i > 0 ? ordered[i - 1] : null;
            var next = i + 1 < ordered.Count ? ordered[i + 1] : null;
            item.ShowAvatar = !item.IsOwn && (prev == null || prev.Message.SenderUid != item.Message.SenderUid);
            item.ShowTime = next == null || next.Message.SenderUid != item.Message.SenderUid || item.IsPending || item.IsFailed;
        }

        if (SignaturesEqual(Messages, ordered))
        {
            return;
        }

        Messages.Clear();
        foreach (var item in ordered)
        {
            Messages.Add(item);
        }
    }

    private static bool SignaturesEqual(IReadOnlyList<MessageItemViewModel> a, IReadOnlyList<MessageItemViewModel> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            var x = a[i];
            var y = b[i];
            if (x.Message.MessageId != y.Message.MessageId) return false;
            if (x.Message.Read != y.Message.Read) return false;
            if (x.Message.Deleted != y.Message.Deleted) return false;
            if (x.IsPending != y.IsPending) return false;
            if (x.IsFailed != y.IsFailed) return false;
        }
        return true;
    }

    private async Task<UserProfile> GetProfileAsync(string uid)
    {
        if (_profiles.TryGetValue(uid, out var cached))
        {
            return cached;
        }

        var profile = await _friends.GetProfileAsync(uid);
        if (profile == null)
        {
            profile = new UserProfile { Uid = uid, DisplayName = uid, Username = uid };
        }
        _profiles[uid] = profile;
        return profile;
    }

    private void ApplyFilter()
    {
        var q = SearchText.Trim().ToLowerInvariant();
        var visible = _allItems
            .Where(i => string.IsNullOrEmpty(q) ||
                        i.DisplayName.ToLowerInvariant().Contains(q) ||
                        i.Username.ToLowerInvariant().Contains(q))
            .ToList();

        var same = visible.Count == Conversations.Count &&
                   visible.Zip(Conversations, (a, b) => a == b).All(match => match);

        if (same) return;

        Conversations.Clear();
        foreach (var item in visible)
        {
            Conversations.Add(item);
        }
    }

    [RelayCommand]
    private async Task OpenConversationAsync(ConversationItemViewModel? item)
    {
        if (item == null) return;

        Selected = item;
        HasOpened = true;
        _loadedConversationId = null;
        ErrorMessage = null;

        try
        {
            await RefreshChatAsync();
            ScrollToBottomRequested?.Invoke(true);
        }
        catch (Exception ex)
        {
            AppErrors.Log("MessagesViewModel", ex);
            ErrorMessage = AppErrors.ToMessage(ex);
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        var text = MessageText.Trim();
        if (string.IsNullOrEmpty(text) || Selected == null) return;

        MessageText = "";
        var conversation = Selected.Conversation;

        var local = new MessageItemViewModel(new Message
        {
            MessageId = _messaging.GenerateMessageId(),
            SenderUid = Me,
            Text = text,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Read = true
        }, Me)
        {
            IsPending = true
        };

        _loadedItems.Add(local);
        RebuildMessages();
        ScrollToBottomRequested?.Invoke(true);

        await SendOrRetryAsync(conversation, local);
    }

    private async Task SendOrRetryAsync(ConversationInfo conversation, MessageItemViewModel item)
    {
        if (item.IsDeleted) return;

        item.IsPending = true;
        item.IsFailed = false;

        try
        {
            await _messaging.SendMessageAsync(conversation, item.Message.Text, item.Message.MessageId);
            item.IsPending = false;
            await RefreshConversationsAsync();
        }
        catch (Exception ex)
        {
            item.IsPending = false;
            item.IsFailed = true;
            AppErrors.Log("MessagesViewModel", ex);
        }
    }

    [RelayCommand]
    private Task RetryMessageAsync(MessageItemViewModel? item)
    {
        if (item == null || Selected == null || item.IsPending) return Task.CompletedTask;
        return SendOrRetryAsync(Selected.Conversation, item);
    }

    [RelayCommand]
    private async Task DeleteMessageAsync(MessageItemViewModel? item)
    {
        if (item == null || Selected == null || !item.CanDelete) return;

        try
        {
            await _messaging.DeleteMessageAsync(Selected.Conversation, item.Message.MessageId);
            item.ApplyDeleted();
            await RefreshConversationsAsync();
        }
        catch (Exception ex)
        {
            AppErrors.Log("MessagesViewModel", ex);
            ErrorMessage = AppErrors.ToMessage(ex);
        }
    }

    [RelayCommand]
    private async Task LoadOlderMessagesAsync()
    {
        if (Selected == null || _isLoadingOlder || !HasMore) return;
        _isLoadingOlder = true;

        try
        {
            var oldest = _loadedItems.Count > 0 ? _loadedItems.Min(x => x.Message.CreatedAt) : 0;
            if (oldest <= 0)
            {
                HasMore = false;
                return;
            }

            var batch = await _messaging.GetMessagesAsync(
                Selected.Conversation.ConversationId,
                MessagingService.PageSize,
                oldest);

            var existingIds = new HashSet<string>(_loadedItems.Select(x => x.Message.MessageId));
            foreach (var message in batch)
            {
                if (!existingIds.Contains(message.MessageId))
                {
                    _loadedItems.Add(new MessageItemViewModel(message, Me));
                }
            }

            if (batch.Count < MessagingService.PageSize)
            {
                _allOlderLoaded = true;
                HasMore = false;
            }

            RebuildMessages();
            OlderMessagesLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            AppErrors.Log("MessagesViewModel", ex);
            ErrorMessage = AppErrors.ToMessage(ex);
        }
        finally
        {
            _isLoadingOlder = false;
        }
    }

    [RelayCommand]
    private async Task OpenNewChatAsync()
    {
        ErrorMessage = null;
        IsNewChatDialogOpen = true;

        try
        {
            var friends = await _friends.GetFriendsAsync();
            NewChatFriends.Clear();
            foreach (var friend in friends)
            {
                NewChatFriends.Add(friend);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = AppErrors.ToMessage(ex);
        }
    }

    [RelayCommand]
    private void CloseNewChat()
    {
        IsNewChatDialogOpen = false;
    }

    [RelayCommand]
    private async Task StartChatAsync(FriendInfo? friend)
    {
        if (friend == null) return;

        try
        {
            var conversation = await _messaging.GetOrCreateConversationAsync(friend.Uid);
            var item = _allItems.FirstOrDefault(i => i.Conversation.ConversationId == conversation.ConversationId);
            if (item == null)
            {
                item = new ConversationItemViewModel(conversation, Me);
                _allItems.Add(item);
            }
            item.Update(conversation);
            item.SetProfile(await GetProfileAsync(conversation.PeerUid(Me)));
            ApplyFilter();

            IsNewChatDialogOpen = false;
            await OpenConversationAsync(item);
        }
        catch (Exception ex)
        {
            AppErrors.Log("MessagesViewModel", ex);
            ErrorMessage = AppErrors.ToMessage(ex);
        }
    }

    [RelayCommand]
    private void CopyUsername(ConversationItemViewModel? item)
    {
        if (item == null) return;
        System.Windows.Clipboard.SetText($"@{item.Username}");
    }

    public static string FormatTime(long milliseconds)
    {
        if (milliseconds <= 0) return "";

        var dt = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).ToLocalTime();
        var now = DateTime.Now;

        if (dt.Date == now.Date) return dt.ToString("HH:mm");
        if (dt.Date == now.Date.AddDays(-1)) return "dün";
        return dt.ToString("dd.MM.yyyy");
    }

    public static string DayText(DateTime day)
    {
        var today = DateTime.Today;
        if (day == today) return "Bugün";
        if (day == today.AddDays(-1)) return "Dün";
        return day.ToString("dd.MM.yyyy");
    }
}

public partial class ConversationItemViewModel : ObservableObject
{
    private readonly string _me;

    public ConversationInfo Conversation { get; }

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _lastMessageText = "";

    [ObservableProperty]
    private string _lastMessageTimeText = "";

    [ObservableProperty]
    private long _unreadCount;

    [ObservableProperty]
    private bool _isSelected;

    private string _status = Presence.Offline;
    private long _lastSeen;
    private bool _hidePresence;

    public string PeerUid => Conversation.PeerUid(_me);
    public string Initial => string.IsNullOrWhiteSpace(DisplayName) ? "?" : DisplayName.Trim()[..1].ToUpperInvariant();
    public bool IsUnread => UnreadCount > 0;
    public string UnreadText => UnreadCount > 99 ? "99+" : UnreadCount.ToString();
    public bool IsOnline => !_hidePresence && Presence.IsPresent(_status, _lastSeen);

    public ConversationItemViewModel(ConversationInfo conversation, string me)
    {
        Conversation = conversation;
        _me = me;
    }

    public void Update(ConversationInfo conversation)
    {
        Conversation.UnreadA = conversation.UnreadA;
        Conversation.UnreadB = conversation.UnreadB;
        Conversation.LastMessage = conversation.LastMessage;
        Conversation.LastMessageTime = conversation.LastMessageTime;
        Conversation.LastSenderUid = conversation.LastSenderUid;
        Conversation.CreatedAt = conversation.CreatedAt;

        UnreadCount = conversation.MyUnread(_me);
        LastMessageTimeText = MessagesViewModel.FormatTime(conversation.LastMessageTime);
        LastMessageText = conversation.LastSenderUid == _me && !string.IsNullOrEmpty(conversation.LastMessage)
            ? $"Sen: {conversation.LastMessage}"
            : conversation.LastMessage;
    }

    public void SetProfile(UserProfile profile)
    {
        DisplayName = profile.DisplayName;
        Username = profile.Username;
        _status = profile.Status;
        _lastSeen = profile.LastSeen;
        _hidePresence = !profile.Privacy.ShowStatus;
        OnPropertyChanged(nameof(IsOnline));
    }

    public void ApplyUnread(long count)
    {
        UnreadCount = count;
    }

    partial void OnUnreadCountChanged(long value)
    {
        OnPropertyChanged(nameof(IsUnread));
        OnPropertyChanged(nameof(UnreadText));
    }
}

public partial class MessageItemViewModel : ObservableObject
{
    public Message Message { get; private set; }
    public bool IsOwn { get; }

    [ObservableProperty]
    private bool _isRead;

    [ObservableProperty]
    private bool _isPending;

    [ObservableProperty]
    private bool _isFailed;

    [ObservableProperty]
    private string _senderName = "";

    [ObservableProperty]
    private bool _showAvatar;

    [ObservableProperty]
    private bool _showTime;

    [ObservableProperty]
    private bool _showDaySeparator;

    [ObservableProperty]
    private string _daySeparatorText = "";

    public string DisplayText => Message.Deleted ? "Bu mesaj silindi" : Message.Text;
    public bool IsDeleted => Message.Deleted;
    public bool CanDelete => IsOwn && !Message.Deleted && !IsPending && !IsFailed;
    public bool CanRetry => IsFailed && !IsPending;
    public string TimeText => MessagesViewModel.FormatTime(Message.CreatedAt);
    public string ReadIndicator => IsRead ? "✓✓" : "✓";
    public string Initial => string.IsNullOrWhiteSpace(SenderName) ? "?" : SenderName.Trim()[..1].ToUpperInvariant();

    public MessageItemViewModel(Message message, string me)
    {
        Message = message;
        IsOwn = message.SenderUid == me;
        _isRead = message.Read;
    }

    public bool UpdateFromServer(Message fresh)
    {
        var changed = false;

        if (Message.Read != fresh.Read || Message.Deleted != fresh.Deleted || Message.Text != fresh.Text || IsPending || IsFailed)
        {
            changed = true;
        }

        Message.Read = fresh.Read;
        Message.ReadAt = fresh.ReadAt;
        Message.Deleted = fresh.Deleted;
        Message.DeletedAt = fresh.DeletedAt;

        if (IsPending || IsFailed)
        {
            IsPending = false;
            IsFailed = false;
        }

        IsRead = Message.Read;
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanRetry));
        return changed;
    }

    public void ApplyRead(bool read)
    {
        Message.Read = read;
        IsRead = read;
    }

    public void ApplyDeleted()
    {
        Message.Deleted = true;
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanRetry));
    }

    public void ApplyDaySeparator(bool show, string text)
    {
        ShowDaySeparator = show;
        DaySeparatorText = text;
    }

    partial void OnIsReadChanged(bool value)
    {
        OnPropertyChanged(nameof(ReadIndicator));
    }

    partial void OnIsPendingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanRetry));
    }

    partial void OnIsFailedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanRetry));
    }
}
