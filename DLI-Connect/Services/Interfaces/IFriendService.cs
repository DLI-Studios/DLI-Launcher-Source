using DLI.Connect.Models;

namespace DLI.Connect.Services.Interfaces;

public interface IFriendService
{
    Task<List<FriendInfo>> GetFriendsAsync();
    Task<List<FriendInfo>> GetPendingRequestsAsync();
    Task<List<FriendInfo>> GetSentRequestsAsync();
    Task<UserProfile?> GetProfileAsync(string uid);
    Task SendFriendRequestAsync(string targetUid);
    Task AcceptFriendRequestAsync(string requesterUid);
    Task DeclineFriendRequestAsync(string requesterUid);
    Task RemoveFriendAsync(string friendUid);
    Task CancelSentRequestAsync(string targetUid);
    Task<List<UserProfile>> SearchUsersAsync(string query, string excludeUid, int limit = 20);
    Task<RequestRelationState> GetRelationStateAsync(string targetUid);
    Task<List<FriendRequest>> GetIncomingRequestsAsync();
    Task AcceptRequestAsync(string requestId, string fromUid);
    Task DeclineRequestAsync(string requestId);
}