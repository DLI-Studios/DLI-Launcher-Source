using DLI.Connect.Firebase;
using DLI.Connect.Models;

namespace DLI.Connect.Services.Interfaces;

public interface IFirebaseFirestore
{
    // Users
    Task<UserProfile?> GetUserAsync(string uid);
    Task CreateUserAsync(string uid, string username, string displayName, string email);
    Task UpdateStatusAsync(string uid, string status);
    Task UpdateProfileAsync(string uid, string? displayName = null, string? avatar = null, string? bio = null);
    Task UpdateSettingsAsync(string uid, string? theme = null, UserPrivacy? privacy = null, UserNotifications? notifications = null);
    Task<bool> IsUsernameTakenAsync(string username);
    Task<List<UserProfile>> SearchUsersAsync(string query, string excludeUid, int limit = 20);
    Task<List<UserProfile>> ListAllUsersAsync();

    // Friend requests
    Task<FriendRequest?> GetFriendRequestAsync(string fromUid, string toUid);
    Task CreateFriendRequestAsync(string fromUid, string toUid);
    Task<List<FriendRequest>> QueryFriendRequestsAsync(string toUid, string status);

    // Friendships
    Task CreateFriendshipAsync(string uid, string friendUid);
    Task<bool> FriendshipExistsAsync(string uid, string friendUid);
    Task<List<string>> ListFriendUidsAsync(string uid);
    Task DeleteDocumentAsync(string path);

    // Conversations
    Task<List<ConversationInfo>> QueryConversationsAsync(string uid, int limit = 200);
    Task<ConversationInfo?> GetConversationAsync(string conversationId);
    Task CreateConversationAsync(ConversationInfo conversation);
    Task UpdateConversationFieldsAsync(string conversationId, Dictionary<string, object> fields);

    // Messages
    Task<List<Message>> QueryMessagesAsync(string conversationId, int limit = 60, long beforeCreatedAt = 0);
    Task SoftDeleteMessageAsync(string conversationId, string messageId);

    // Typing
    Task SetTypingAsync(string conversationId, string uid);
    Task<long> GetTypingAtAsync(string conversationId, string uid);

    // Commits
    Task CommitAsync(IReadOnlyList<CommitWrite> writes);

    // Parties
    Task<Party?> GetPartyAsync(string partyId);
    Task<Party?> GetUserPartyAsync(string uid);
    Task<string> CreatePartyAsync(string leaderUid, string leaderDisplayName, string leaderUsername, string leaderAvatar);
    Task UpdatePartyAsync(string partyId, Dictionary<string, object> fields);
    Task DeletePartyAsync(string partyId);
    Task<PartyInvite?> GetPartyInviteAsync(string inviteId);
    Task CreatePartyInviteAsync(PartyInvite invite);
    Task UpdatePartyInviteAsync(string inviteId, Dictionary<string, object> fields);
    Task<List<PartyInvite>> QueryPartyInvitesAsync(string toUid, PartyInviteStatus? status = null);
    Task ListenPartyAsync(string partyId, Action<Party> onChange);
    Task ListenPartyInvitesAsync(string toUid, Action<List<PartyInvite>> onChange);
    Task StopListenPartyAsync(string partyId);
    Task StopListenPartyInvitesAsync(string toUid);

    // Voice Sessions
    Task<Party?> GetVoiceSessionAsync(string partyId);
    Task UpdateVoiceSessionAsync(string partyId, Dictionary<string, object> fields);
    Task DeleteVoiceSessionAsync(string partyId);
    Task ListenVoiceSessionAsync(string partyId, Action<Party> onChange);

    // Voice Signaling
    Task<VoiceSignalDoc?> GetVoiceSignalAsync(string partyId, string signalDocId);
    Task UpdateVoiceSignalAsync(string partyId, string signalDocId, Dictionary<string, object> fields);
    Task DeleteVoiceSignalAsync(string partyId, string signalDocId);
    Task ListenVoiceSignalAsync(string partyId, string signalDocId, Action<VoiceSignalDoc> onChange);
}
