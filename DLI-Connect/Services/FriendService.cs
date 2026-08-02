using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DLI.Connect.Firebase;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.Services;

public class FriendService : IFriendService
{
    private readonly IFirebaseFirestore _db;
    private readonly ISessionManager _session;

    public FriendService(IFirebaseFirestore db, ISessionManager session)
    {
        _db = db;
        _session = session;
    }

    private string Me => _session.CurrentUser?.Uid
        ?? throw new InvalidOperationException("Oturum açık değil.");

    public async Task<List<UserProfile>> SearchUsersAsync(string query, string excludeUid, int limit = 20)
    {
        var q = query.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q))
        {
            return new List<UserProfile>();
        }

        return await _db.SearchUsersAsync(q, excludeUid, limit);
    }

    public async Task<RequestRelationState> GetRelationStateAsync(string targetUid)
    {
        if (await _db.FriendshipExistsAsync(Me, targetUid))
        {
            return RequestRelationState.AlreadyFriends;
        }

        var sent = await _db.GetFriendRequestAsync(Me, targetUid);
        if (sent != null && sent.Status == "pending")
        {
            return RequestRelationState.RequestSent;
        }

        var incoming = await _db.GetFriendRequestAsync(targetUid, Me);
        if (incoming != null && incoming.Status == "pending")
        {
            return RequestRelationState.IncomingRequest;
        }

        return RequestRelationState.None;
    }

    public async Task SendFriendRequestAsync(string targetUid)
    {
        if (targetUid == Me)
        {
            throw new InvalidOperationException("Kendine arkadaşlık isteği gönderemezsin.");
        }
        if (await _db.FriendshipExistsAsync(Me, targetUid))
        {
            throw new InvalidOperationException("Bu kullanıcı zaten arkadaşın.");
        }

        var target = await _db.GetUserAsync(targetUid);
        if (target != null && !CanRequestTo(target))
        {
            throw new InvalidOperationException("Bu kullanıcı arkadaşlık isteği almıyor.");
        }

        var sent = await _db.GetFriendRequestAsync(Me, targetUid);
        if (sent != null && sent.Status == "pending")
        {
            throw new InvalidOperationException("Bu kullanıcıya zaten istek gönderdin.");
        }

        var incoming = await _db.GetFriendRequestAsync(targetUid, Me);
        if (incoming != null && incoming.Status == "pending")
        {
            throw new InvalidOperationException("Bu kullanıcı sana istek göndermiş. Arkadaşlık istekleri sayfasından kabul edebilirsin.");
        }

        await _db.CreateFriendRequestAsync(Me, targetUid);
    }

    private static bool CanRequestTo(UserProfile target) =>
        target.Privacy.FriendRequests switch
        {
            "nobody" => false,
            _ => true
        };

    public async Task<List<FriendRequest>> GetIncomingRequestsAsync()
    {
        if (_session.CurrentUser == null) return new List<FriendRequest>();
        return await _db.QueryFriendRequestsAsync(Me, "pending");
    }

    public async Task<UserProfile?> GetProfileAsync(string uid) =>
        await _db.GetUserAsync(uid);

    public async Task AcceptRequestAsync(string requestId, string fromUid)
    {
        if (requestId != $"{fromUid}_{Me}")
        {
            throw new InvalidOperationException("Geçersiz istek.");
        }

        await _db.CreateFriendshipAsync(Me, fromUid);
        await _db.CreateFriendshipAsync(fromUid, Me);
        await _db.DeleteDocumentAsync($"friend_requests/{requestId}");
    }

    public async Task DeclineRequestAsync(string requestId) =>
        await _db.DeleteDocumentAsync($"friend_requests/{requestId}");

    public async Task<List<FriendInfo>> GetFriendsAsync()
    {
        if (_session.CurrentUser == null) return new List<FriendInfo>();

        var friendUids = await _db.ListFriendUidsAsync(Me);
        var friends = new List<FriendInfo>();

        foreach (var uid in friendUids)
        {
            var profile = await _db.GetUserAsync(uid);
            if (profile == null) continue;

            friends.Add(new FriendInfo
            {
                Uid = uid,
                Username = profile.Username,
                DisplayName = profile.DisplayName,
                Avatar = profile.Avatar,
                Status = profile.Status,
                LastSeen = profile.LastSeen,
                HidePresence = !profile.Privacy.ShowStatus
            });
        }

        return friends
            .OrderByDescending(f => f.IsOnline)
            .ThenBy(f => f.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task RemoveFriendAsync(string friendUid)
    {
        if (friendUid == Me) return;

        await _db.DeleteDocumentAsync($"friends/{Me}/friends/{friendUid}");
        await _db.DeleteDocumentAsync($"friends/{friendUid}/friends/{Me}");
    }

    public async Task<List<FriendInfo>> GetPendingRequestsAsync()
    {
        var requests = await GetIncomingRequestsAsync();
        var result = new List<FriendInfo>();
        foreach (var req in requests)
        {
            var profile = await _db.GetUserAsync(req.FromUid);
            if (profile != null)
            {
                result.Add(new FriendInfo
                {
                    Uid = req.FromUid,
                    Username = profile.Username,
                    DisplayName = profile.DisplayName,
                    Avatar = profile.Avatar,
                    Status = profile.Status,
                    LastSeen = profile.LastSeen,
                    HidePresence = !profile.Privacy.ShowStatus
                });
            }
        }
        return result;
    }

    public async Task<List<FriendInfo>> GetSentRequestsAsync()
    {
        if (_session.CurrentUser == null) return new List<FriendInfo>();
        var requests = await _db.QueryFriendRequestsAsync(Me, "pending");
        var result = new List<FriendInfo>();
        foreach (var req in requests)
        {
            var profile = await _db.GetUserAsync(req.ToUid);
            if (profile != null)
            {
                result.Add(new FriendInfo
                {
                    Uid = req.ToUid,
                    Username = profile.Username,
                    DisplayName = profile.DisplayName,
                    Avatar = profile.Avatar,
                    Status = profile.Status,
                    LastSeen = profile.LastSeen,
                    HidePresence = !profile.Privacy.ShowStatus
                });
            }
        }
        return result;
    }

    public async Task AcceptFriendRequestAsync(string requesterUid)
    {
        var requestId = $"{requesterUid}_{Me}";
        await AcceptRequestAsync(requestId, requesterUid);
    }

    public async Task DeclineFriendRequestAsync(string requesterUid)
    {
        var requestId = $"{requesterUid}_{Me}";
        await DeclineRequestAsync(requestId);
    }

    public async Task CancelSentRequestAsync(string targetUid)
    {
        var requestId = $"{Me}_{targetUid}";
        await _db.DeleteDocumentAsync($"friend_requests/{requestId}");
    }
}