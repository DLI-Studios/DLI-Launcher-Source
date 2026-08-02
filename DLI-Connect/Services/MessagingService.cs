using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DLI.Connect.Firebase;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.Services;

public class MessagingService : IMessagingService
{
    private readonly IFirebaseFirestore _db;
    private readonly ISessionManager _session;

    public MessagingService(IFirebaseFirestore db, ISessionManager session)
    {
        _db = db;
        _session = session;
    }

    private string Me => _session.CurrentUser?.Uid
        ?? throw new InvalidOperationException("Oturum açık değil.");

    private static string DocName(string path) =>
        $"projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/{path}";

    public static string ConversationIdFor(string uidA, string uidB)
    {
        var parts = new[] { uidA, uidB };
        Array.Sort(parts, StringComparer.Ordinal);
        return $"{parts[0]}_{parts[1]}";
    }

    public async Task<ConversationInfo> GetOrCreateConversationAsync(string peerUid)
    {
        if (peerUid == Me)
        {
            throw new InvalidOperationException("Kendinle mesajlaşamazsın.");
        }
        if (!await _db.FriendshipExistsAsync(Me, peerUid))
        {
            throw new InvalidOperationException("Sadece arkadaşlarınla mesajlaşabilirsin.");
        }

        var conversationId = ConversationIdFor(Me, peerUid);
        var existing = await _db.GetConversationAsync(conversationId);
        if (existing != null)
        {
            return existing;
        }

        var parts = new[] { Me, peerUid };
        Array.Sort(parts, StringComparer.Ordinal);

        var conversation = new ConversationInfo
        {
            ConversationId = conversationId,
            ParticipantA = parts[0],
            ParticipantB = parts[1],
            Participants = new List<string> { parts[0], parts[1] }
        };

        await _db.CreateConversationAsync(conversation);
        return conversation;
    }

    public async Task<List<ConversationInfo>> GetConversationsAsync()
    {
        if (_session.CurrentUser == null) return new List<ConversationInfo>();

        var conversations = await _db.QueryConversationsAsync(Me, 200);

        return conversations
            .OrderByDescending(c => c.LastMessageTime)
            .ToList();
    }

    public const int PageSize = 60;

    public string GenerateMessageId() =>
        $"{Me}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";

    public async Task SendMessageAsync(ConversationInfo conversation, string text, string? messageId = null)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        messageId ??= GenerateMessageId();
        var peerUnreadField = conversation.IsParticipantA(Me) ? "unreadB" : "unreadA";

        var writes = new List<CommitWrite>
        {
            new()
            {
                Name = DocName($"conversations/{conversation.ConversationId}/messages/{messageId}"),
                Fields = new Dictionary<string, object>
                {
                    ["messageId"] = new Dictionary<string, object> { ["stringValue"] = messageId },
                    ["senderUid"] = new Dictionary<string, object> { ["stringValue"] = Me },
                    ["text"] = new Dictionary<string, object> { ["stringValue"] = text },
                    ["createdAt"] = new Dictionary<string, object> { ["integerValue"] = now.ToString() },
                    ["read"] = new Dictionary<string, object> { ["booleanValue"] = false },
                    ["readAt"] = new Dictionary<string, object> { ["integerValue"] = "0" },
                    ["deleted"] = new Dictionary<string, object> { ["booleanValue"] = false },
                    ["deletedAt"] = new Dictionary<string, object> { ["integerValue"] = "0" }
                },
                FieldPaths = new List<string> { "messageId", "senderUid", "text", "createdAt", "read", "readAt", "deleted", "deletedAt" }
            },
            new()
            {
                Name = DocName($"conversations/{conversation.ConversationId}"),
                Fields = new Dictionary<string, object>
                {
                    ["lastMessage"] = new Dictionary<string, object> { ["stringValue"] = text },
                    ["lastMessageTime"] = new Dictionary<string, object> { ["integerValue"] = now.ToString() },
                    ["lastSenderUid"] = new Dictionary<string, object> { ["stringValue"] = Me }
                },
                FieldPaths = new List<string> { "lastMessage", "lastMessageTime", "lastSenderUid" },
                Transforms = new List<FieldTransform>
                {
                    new() { FieldPath = peerUnreadField, Increment = 1 }
                }
            }
        };

        await _db.CommitAsync(writes);
    }

    public Task<List<Message>> GetMessagesAsync(string conversationId) =>
        GetMessagesAsync(conversationId, PageSize);

    public async Task<List<Message>> GetMessagesAsync(string conversationId, int limit, long beforeCreatedAt = 0)
    {
        var messages = await _db.QueryMessagesAsync(conversationId, limit, beforeCreatedAt);
        return messages.OrderBy(m => m.CreatedAt).ToList();
    }

    public Task DeleteMessageAsync(ConversationInfo conversation, string messageId) =>
        _db.SoftDeleteMessageAsync(conversation.ConversationId, messageId);

    public async Task MarkReadAsync(ConversationInfo conversation, List<Message> messages)
    {
        if (_session.CurrentUser == null) return;

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var myUnreadField = conversation.IsParticipantA(Me) ? "unreadA" : "unreadB";

        var writes = new List<CommitWrite>
        {
            new()
            {
                Name = DocName($"conversations/{conversation.ConversationId}"),
                Fields = new Dictionary<string, object>
                {
                    [myUnreadField] = new Dictionary<string, object> { ["integerValue"] = "0" }
                },
                FieldPaths = new List<string> { myUnreadField }
            }
        };

        foreach (var message in messages.Where(m => m.SenderUid != Me && !m.Read))
        {
            writes.Add(new CommitWrite
            {
                Name = DocName($"conversations/{conversation.ConversationId}/messages/{message.MessageId}"),
                Fields = new Dictionary<string, object>
                {
                    ["read"] = new Dictionary<string, object> { ["booleanValue"] = true },
                    ["readAt"] = new Dictionary<string, object> { ["integerValue"] = now.ToString() }
                },
                FieldPaths = new List<string> { "read", "readAt" }
            });
        }

        await _db.CommitAsync(writes);
    }

    public Task SetTypingAsync(string conversationId) =>
        _db.SetTypingAsync(conversationId, Me);

    public async Task<bool> IsPeerTypingAsync(ConversationInfo conversation)
    {
        var peerTypingAt = await _db.GetTypingAtAsync(conversation.ConversationId, conversation.PeerUid(Me));
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return peerTypingAt > 0 && now - peerTypingAt < 4000;
    }
}