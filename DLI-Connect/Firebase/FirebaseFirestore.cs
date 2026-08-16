using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.Firebase;

public class FirebaseFirestore : IFirebaseFirestore
{
    private readonly IFirebaseClient _client;

    public FirebaseFirestore(IFirebaseClient client)
    {
        _client = client;
    }

    private static string Url(string path) =>
        path.Contains('?')
            ? $"{FirebaseConfig.FirestoreBaseUrl}/{path}&key={FirebaseConfig.ApiKey}"
            : $"{FirebaseConfig.FirestoreBaseUrl}/{path}?key={FirebaseConfig.ApiKey}";

    public async Task CreateUserAsync(string uid, string username, string displayName, string email)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var body = new
        {
            fields = new
            {
                uid = Field("stringValue", uid),
                username = Field("stringValue", username),
                displayName = Field("stringValue", displayName),
                email = Field("stringValue", email),
                avatar = Field("stringValue", ""),
                bio = Field("stringValue", ""),
                status = Field("stringValue", "offline"),
                theme = Field("stringValue", "dark"),
                privacy = MapField(new Dictionary<string, object>
                {
                    ["friendRequests"] = Field("stringValue", "everyone"),
                    ["showStatus"] = Field("booleanValue", "true"),
                    ["showActivity"] = Field("booleanValue", "true")
                }),
                notifications = MapField(new Dictionary<string, object>
                {
                    ["enabled"] = Field("booleanValue", "true"),
                    ["friendRequests"] = Field("booleanValue", "true"),
                    ["messages"] = Field("booleanValue", "true"),
                    ["partyInvites"] = Field("booleanValue", "true")
                }),
                createdAt = Field("integerValue", now.ToString()),
                lastSeen = Field("integerValue", now.ToString())
            }
        };

        await _client.PatchAsync(Url($"users/{uid}?updateMask.fieldPaths=uid&updateMask.fieldPaths=username&updateMask.fieldPaths=displayName&updateMask.fieldPaths=email&updateMask.fieldPaths=avatar&updateMask.fieldPaths=bio&updateMask.fieldPaths=status&updateMask.fieldPaths=theme&updateMask.fieldPaths=privacy&updateMask.fieldPaths=notifications&updateMask.fieldPaths=createdAt&updateMask.fieldPaths=lastSeen"), body);
    }

    public async Task<UserProfile?> GetUserAsync(string uid)
    {
        try
        {
            var json = await _client.GetAsync(Url($"users/{uid}"));
            return ParseDocument(json);
        }
        catch (FirebaseApiException ex) when (ex.ErrorCode == "NOT_FOUND")
        {
            return null;
        }
    }

    public async Task UpdateStatusAsync(string uid, string status)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var body = new
        {
            fields = new
            {
                status = Field("stringValue", status),
                lastSeen = Field("integerValue", now.ToString())
            }
        };

        await _client.PatchAsync(Url($"users/{uid}?updateMask.fieldPaths=status&updateMask.fieldPaths=lastSeen"), body);
    }

    public async Task UpdateProfileAsync(string uid, string? displayName = null, string? avatar = null, string? bio = null)
    {
        var fields = new Dictionary<string, object>();
        var mask = new List<string>();

        if (displayName != null) { fields["displayName"] = Field("stringValue", displayName); mask.Add("displayName"); }
        if (avatar != null) { fields["avatar"] = Field("stringValue", avatar); mask.Add("avatar"); }
        if (bio != null) { fields["bio"] = Field("stringValue", bio); mask.Add("bio"); }

        if (fields.Count == 0) return;

        var maskQuery = string.Join("&", mask.Select(m => $"updateMask.fieldPaths={m}"));
        var body = new { fields };

        await _client.PatchAsync(Url($"users/{uid}?{maskQuery}"), body);
    }

    public async Task UpdateSettingsAsync(string uid, string? theme = null, UserPrivacy? privacy = null, UserNotifications? notifications = null)
    {
        var fields = new Dictionary<string, object>();
        var mask = new List<string>();

        if (theme != null)
        {
            fields["theme"] = Field("stringValue", theme);
            mask.Add("theme");
        }

        if (privacy != null)
        {
            fields["privacy"] = MapField(new Dictionary<string, object>
            {
                ["friendRequests"] = Field("stringValue", privacy.FriendRequests),
                ["showStatus"] = Field("booleanValue", privacy.ShowStatus ? "true" : "false"),
                ["showActivity"] = Field("booleanValue", privacy.ShowActivity ? "true" : "false")
            });
            mask.Add("privacy");
        }

        if (notifications != null)
        {
            fields["notifications"] = MapField(new Dictionary<string, object>
            {
                ["enabled"] = Field("booleanValue", notifications.Enabled ? "true" : "false"),
                ["friendRequests"] = Field("booleanValue", notifications.FriendRequests ? "true" : "false"),
                ["messages"] = Field("booleanValue", notifications.Messages ? "true" : "false"),
                ["partyInvites"] = Field("booleanValue", notifications.PartyInvites ? "true" : "false")
            });
            mask.Add("notifications");
        }

        if (fields.Count == 0) return;

        var maskQuery = string.Join("&", mask.Select(m => $"updateMask.fieldPaths={m}"));
        await _client.PatchAsync(Url($"users/{uid}?{maskQuery}"), new { fields });
    }

    private static Dictionary<string, object> Field(string type, string value) =>
        new() { [type] = value };

    private static Dictionary<string, object> MapField(Dictionary<string, object> fields) =>
        new() { ["mapValue"] = new Dictionary<string, object> { ["fields"] = fields } };

    // ---- Commits (atomic writes + server-side transforms) ----

    public async Task CommitAsync(IReadOnlyList<CommitWrite> writes)
    {
        var writeObjects = writes.Select(w =>
        {
            var obj = new Dictionary<string, object>
            {
                ["update"] = new Dictionary<string, object>
                {
                    ["name"] = w.Name,
                    ["fields"] = w.Fields ?? new Dictionary<string, object>()
                }
            };

            if (w.FieldPaths is { Count: > 0 })
            {
                obj["updateMask"] = new Dictionary<string, object> { ["fieldPaths"] = w.FieldPaths };
            }

            if (w.Transforms is { Count: > 0 })
            {
                obj["updateTransforms"] = w.Transforms.Select(t =>
                {
                    var tObj = new Dictionary<string, object> { ["fieldPath"] = t.FieldPath };
                    if (t.Increment.HasValue)
                    {
                        tObj["increment"] = new Dictionary<string, object> { ["integerValue"] = t.Increment.Value.ToString() };
                    }
                    if (t.SetToServerValue != null)
                    {
                        tObj["setToServerValue"] = t.SetToServerValue;
                    }
                    return tObj;
                }).ToList();
            }

            return obj;
        }).ToList();

        var url = $"{FirebaseConfig.FirestoreBaseUrl}:commit?key={FirebaseConfig.ApiKey}";
        await _client.PostAsync(url, new { writes = writeObjects });
    }

    // ---- Queries & collections ----

    public async Task<List<JsonElement>> RunQueryAsync(string collectionId, IReadOnlyList<(string Field, string Op, object Value)> filters, int limit = 200) =>
        await RunQueryAsync(null, collectionId, filters, null, limit);

    public async Task<List<JsonElement>> RunQueryAsync(
        string? parentPath,
        string collectionId,
        IReadOnlyList<(string Field, string Op, object Value)>? filters,
        IReadOnlyList<(string Field, string Direction)>? orderBy,
        int limit = 200)
    {
        var structured = new Dictionary<string, object>
        {
            ["from"] = new[]
            {
                new Dictionary<string, object> { ["collectionId"] = collectionId }
            },
            ["limit"] = limit
        };

        if (filters is { Count: > 0 })
        {
            structured["where"] = BuildWhere(filters);
        }

        if (orderBy is { Count: > 0 })
        {
            structured["orderBy"] = orderBy.Select(o => (object)new Dictionary<string, object>
            {
                ["field"] = new Dictionary<string, object> { ["fieldPath"] = o.Field },
                ["direction"] = o.Direction
            }).ToList();
        }

        var query = new Dictionary<string, object>
        {
            ["structuredQuery"] = structured
        };

        var url = parentPath == null
            ? Url(":runQuery")
            : Url($"{parentPath}:runQuery");

        var json = await _client.PostAsync(url, query);

        var documents = new List<JsonElement>();
        if (json.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in json.EnumerateArray())
            {
                if (item.TryGetProperty("document", out var doc))
                {
                    documents.Add(doc.Clone());
                }
            }
        }
        return documents;
    }

    private static object BuildWhere(IReadOnlyList<(string Field, string Op, object Value)> filters)
    {
        if (filters.Count == 0) return new { };

        var list = filters.Select(f => (object)new Dictionary<string, object>
        {
            ["fieldFilter"] = new Dictionary<string, object>
            {
                ["field"] = new Dictionary<string, object> { ["fieldPath"] = f.Field },
                ["op"] = f.Op,
                ["value"] = f.Value switch
                {
                    string s => new Dictionary<string, object> { ["stringValue"] = s },
                    long l => new Dictionary<string, object> { ["integerValue"] = l.ToString() },
                    bool b => new Dictionary<string, object> { ["booleanValue"] = b },
                    _ => new Dictionary<string, object> { ["nullValue"] = 0 }
                }
            }
        }).ToList();

        if (list.Count == 1) return list[0];

        return new Dictionary<string, object>
        {
            ["compositeFilter"] = new Dictionary<string, object>
            {
                ["op"] = "AND",
                ["filters"] = list
            }
        };
    }

    public async Task<List<UserProfile>> ListAllUsersAsync()
    {
        var profiles = new List<UserProfile>();
        string? token = null;

        do
        {
            var url = token == null
                ? Url("users?pageSize=300")
                : Url($"users?pageSize=300&pageToken={Uri.EscapeDataString(token)}");
            var json = await _client.GetAsync(url);

            if (json.TryGetProperty("documents", out var docs))
            {
                foreach (var doc in docs.EnumerateArray())
                {
                    profiles.Add(ParseDocument(doc));
                }
            }

            token = json.TryGetProperty("nextPageToken", out var t) ? t.GetString() : null;
        } while (token != null);

        return profiles;
    }

    public async Task<bool> IsUsernameTakenAsync(string username)
    {
        var users = await ListAllUsersAsync();
        return users.Any(u => u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<UserProfile>> SearchUsersAsync(string query, string excludeUid, int limit = 20)
    {
        var q = query.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q)) return new List<UserProfile>();

        var users = await ListAllUsersAsync();
        return users
            .Where(u => u.Uid != excludeUid && u.Username.ToLowerInvariant().Contains(q))
            .OrderBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Take(limit)
            .ToList();
    }

    public async Task DeleteDocumentAsync(string path) =>
        await _client.DeleteAsync(Url(path));

    // ---- Friend requests ----

    public async Task<FriendRequest?> GetFriendRequestAsync(string fromUid, string toUid)
    {
        try
        {
            var json = await _client.GetAsync(Url($"friend_requests/{fromUid}_{toUid}"));
            return ParseFriendRequest(json, $"{fromUid}_{toUid}");
        }
        catch (FirebaseApiException ex) when (ex.ErrorCode == "NOT_FOUND")
        {
            return null;
        }
    }

    public async Task CreateFriendRequestAsync(string fromUid, string toUid)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var requestId = $"{fromUid}_{toUid}";
        var body = new
        {
            fields = new
            {
                requestId = Field("stringValue", requestId),
                fromUid = Field("stringValue", fromUid),
                toUid = Field("stringValue", toUid),
                status = Field("stringValue", "pending"),
                createdAt = Field("integerValue", now.ToString())
            }
        };

        await _client.PatchAsync(Url($"friend_requests/{requestId}?updateMask.fieldPaths=requestId&updateMask.fieldPaths=fromUid&updateMask.fieldPaths=toUid&updateMask.fieldPaths=status&updateMask.fieldPaths=createdAt"), body);
    }

    public async Task<List<FriendRequest>> QueryFriendRequestsAsync(string toUid, string status)
    {
        var docs = await RunQueryAsync("friend_requests", new[]
        {
            ("toUid", "EQUAL", (object)toUid),
            ("status", "EQUAL", (object)status)
        });

        return docs
            .Select(d => ParseFriendRequest(d, d.TryGetProperty("name", out var name) ? name.GetString()?.Split('/').Last() ?? "" : ""))
            .ToList();
    }

    private static FriendRequest ParseFriendRequest(JsonElement json, string requestId)
    {
        var request = new FriendRequest { RequestId = requestId };
        if (!json.TryGetProperty("fields", out var fields)) return request;

        request.FromUid = GetString(fields, "fromUid");
        request.ToUid = GetString(fields, "toUid");
        request.Status = GetString(fields, "status");
        request.CreatedAt = GetLong(fields, "createdAt");
        return request;
    }

    // ---- Friendships ----

    public async Task CreateFriendshipAsync(string uid, string friendUid)
    {
        var since = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var body = new
        {
            fields = new
            {
                friendUid = Field("stringValue", friendUid),
                since = Field("integerValue", since.ToString())
            }
        };

        await _client.PatchAsync(Url($"friends/{uid}/friends/{friendUid}?updateMask.fieldPaths=friendUid&updateMask.fieldPaths=since"), body);
    }

    public async Task<bool> FriendshipExistsAsync(string uid, string friendUid)
    {
        try
        {
            await _client.GetAsync(Url($"friends/{uid}/friends/{friendUid}"));
            return true;
        }
        catch (FirebaseApiException ex) when (ex.ErrorCode == "NOT_FOUND")
        {
            return false;
        }
    }

    public async Task<List<string>> ListFriendUidsAsync(string uid)
    {
        var uids = new List<string>();
        string? token = null;

        do
        {
            var url = token == null
                ? Url($"friends/{uid}/friends?pageSize=300")
                : Url($"friends/{uid}/friends?pageSize=300&pageToken={Uri.EscapeDataString(token)}");
            var json = await _client.GetAsync(url);

            if (json.TryGetProperty("documents", out var docs))
            {
                foreach (var doc in docs.EnumerateArray())
                {
                    var name = doc.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var id = name.Split('/').LastOrDefault();
                    if (!string.IsNullOrEmpty(id)) uids.Add(id);
                }
            }

            token = json.TryGetProperty("nextPageToken", out var t) ? t.GetString() : null;
        } while (token != null);

        return uids;
    }

    private static UserProfile ParseDocument(JsonElement json)
    {
        var profile = new UserProfile();
        if (!json.TryGetProperty("fields", out var fields)) return profile;

        profile.Uid = GetString(fields, "uid");
        profile.Username = GetString(fields, "username");
        profile.DisplayName = GetString(fields, "displayName");
        profile.Email = GetString(fields, "email");
        profile.Avatar = GetString(fields, "avatar");
        profile.Bio = GetString(fields, "bio");
        profile.Status = GetString(fields, "status");
        profile.Theme = GetString(fields, "theme");
        profile.CreatedAt = GetLong(fields, "createdAt");
        profile.LastSeen = GetLong(fields, "lastSeen");

        if (fields.TryGetProperty("privacy", out var privacy) && privacy.TryGetProperty("mapValue", out var pm))
        {
            var p = pm.GetProperty("fields");
            profile.Privacy.FriendRequests = GetString(p, "friendRequests");
            profile.Privacy.ShowStatus = GetBool(p, "showStatus");
            profile.Privacy.ShowActivity = GetBool(p, "showActivity");
        }

        if (fields.TryGetProperty("notifications", out var notes) && notes.TryGetProperty("mapValue", out var nm))
        {
            var n = nm.GetProperty("fields");
            profile.Notifications.Enabled = GetBool(n, "enabled");
            profile.Notifications.FriendRequests = GetBool(n, "friendRequests");
            profile.Notifications.Messages = GetBool(n, "messages");
            profile.Notifications.PartyInvites = GetBool(n, "partyInvites");
        }

        return profile;
    }

    // ---- Conversations ----

    public async Task<List<ConversationInfo>> QueryConversationsAsync(string uid, int limit = 200)
    {
        var docs = await RunQueryAsync(null, "conversations",
            new[] { ("participants", "ARRAY_CONTAINS", (object)uid) },
            null,
            limit);

        return docs.Select(ParseConversation).ToList();
    }

    public async Task<ConversationInfo?> GetConversationAsync(string conversationId)
    {
        try
        {
            var json = await _client.GetAsync(Url($"conversations/{conversationId}"));
            return ParseConversation(json);
        }
        catch (FirebaseApiException ex) when (ex.ErrorCode == "NOT_FOUND")
        {
            return null;
        }
    }

    public async Task CreateConversationAsync(ConversationInfo conversation)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var body = new
        {
            fields = new
            {
                conversationId = Field("stringValue", conversation.ConversationId),
                participantA = Field("stringValue", conversation.ParticipantA),
                participantB = Field("stringValue", conversation.ParticipantB),
                participants = new Dictionary<string, object>
                {
                    ["arrayValue"] = new Dictionary<string, object>
                    {
                        ["values"] = conversation.Participants
                            .Select(p => (object)new Dictionary<string, object> { ["stringValue"] = p })
                            .ToList()
                    }
                },
                unreadA = Field("integerValue", "0"),
                unreadB = Field("integerValue", "0"),
                hiddenA = new Dictionary<string, object> { ["booleanValue"] = false },
                hiddenB = new Dictionary<string, object> { ["booleanValue"] = false },
                hiddenUntilA = Field("integerValue", "0"),
                hiddenUntilB = Field("integerValue", "0"),
                lastMessage = Field("stringValue", ""),
                lastMessageTime = Field("integerValue", now.ToString()),
                lastSenderUid = Field("stringValue", ""),
                createdAt = Field("integerValue", now.ToString())
            }
        };

        var mask = string.Join("&", new[]
        {
            "conversationId", "participantA", "participantB", "participants", "unreadA", "unreadB",
            "hiddenA", "hiddenB", "hiddenUntilA", "hiddenUntilB", "lastMessage", "lastMessageTime",
            "lastSenderUid", "createdAt"
        }.Select(m => $"updateMask.fieldPaths={m}"));

        await _client.PatchAsync(Url($"conversations/{conversation.ConversationId}?{mask}"), body);
    }

    public async Task UpdateConversationFieldsAsync(string conversationId, Dictionary<string, object> fields)
    {
        if (fields.Count == 0) return;
        var maskQuery = string.Join("&", fields.Keys.Select(m => $"updateMask.fieldPaths={m}"));
        await _client.PatchAsync(Url($"conversations/{conversationId}?{maskQuery}"), new { fields });
    }

    // ---- Messages ----

    public async Task<List<Message>> QueryMessagesAsync(string conversationId, int limit = 60, long beforeCreatedAt = 0)
    {
        List<(string Field, string Op, object Value)>? filters = null;
        if (beforeCreatedAt > 0)
        {
            filters = new List<(string, string, object)> { ("createdAt", "LESS_THAN", beforeCreatedAt) };
        }

        var docs = await RunQueryAsync($"conversations/{conversationId}", "messages",
            filters,
            new[] { ("createdAt", "DESCENDING") },
            limit);

        return docs.Select(ParseMessage).ToList();
    }

    public async Task SoftDeleteMessageAsync(string conversationId, string messageId)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var body = new
        {
            fields = new
            {
                deleted = new Dictionary<string, object> { ["booleanValue"] = true },
                deletedAt = new Dictionary<string, object> { ["integerValue"] = now.ToString() }
            }
        };

        await _client.PatchAsync(
            Url($"conversations/{conversationId}/messages/{messageId}?updateMask.fieldPaths=deleted&updateMask.fieldPaths=deletedAt"),
            body);
    }

    // ---- Typing ----

    public async Task SetTypingAsync(string conversationId, string uid)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var body = new
        {
            fields = new
            {
                uid = Field("stringValue", uid),
                lastTypingAt = Field("integerValue", now.ToString())
            }
        };

        await _client.PatchAsync(Url($"conversations/{conversationId}/typing/{uid}?updateMask.fieldPaths=uid&updateMask.fieldPaths=lastTypingAt"), body);
    }

    public async Task<long> GetTypingAtAsync(string conversationId, string uid)
    {
        try
        {
            var json = await _client.GetAsync(Url($"conversations/{conversationId}/typing/{uid}"));
            if (!json.TryGetProperty("fields", out var fields)) return 0;
            return GetLong(fields, "lastTypingAt");
        }
        catch (FirebaseApiException ex) when (ex.ErrorCode == "NOT_FOUND")
        {
            return 0;
        }
    }

    private static ConversationInfo ParseConversation(JsonElement json)
    {
        var conversation = new ConversationInfo();
        if (!json.TryGetProperty("fields", out var fields)) return conversation;

        conversation.ConversationId = GetString(fields, "conversationId");
        conversation.ParticipantA = GetString(fields, "participantA");
        conversation.ParticipantB = GetString(fields, "participantB");
        conversation.UnreadA = GetLong(fields, "unreadA");
        conversation.UnreadB = GetLong(fields, "unreadB");
        conversation.HiddenA = GetBool(fields, "hiddenA");
        conversation.HiddenB = GetBool(fields, "hiddenB");
        conversation.HiddenUntilA = GetLong(fields, "hiddenUntilA");
        conversation.HiddenUntilB = GetLong(fields, "hiddenUntilB");
        conversation.LastMessage = GetString(fields, "lastMessage");
        conversation.LastMessageTime = GetLong(fields, "lastMessageTime");
        conversation.LastSenderUid = GetString(fields, "lastSenderUid");
        conversation.CreatedAt = GetLong(fields, "createdAt");

        if (fields.TryGetProperty("participants", out var participants) &&
            participants.TryGetProperty("arrayValue", out var array) &&
            array.TryGetProperty("values", out var values))
        {
            foreach (var value in values.EnumerateArray())
            {
                if (value.TryGetProperty("stringValue", out var s))
                {
                    conversation.Participants.Add(s.GetString() ?? "");
                }
            }
        }

        return conversation;
    }

    private static Message ParseMessage(JsonElement json)
    {
        var message = new Message();
        if (!json.TryGetProperty("fields", out var fields)) return message;

        message.MessageId = GetString(fields, "messageId");
        message.SenderUid = GetString(fields, "senderUid");
        message.Text = GetString(fields, "text");
        message.CreatedAt = GetLong(fields, "createdAt");
        message.Read = GetBool(fields, "read");
        message.ReadAt = GetLong(fields, "readAt");
        message.Deleted = GetBool(fields, "deleted");
        message.DeletedAt = GetLong(fields, "deletedAt");
        return message;
    }

    private static bool GetBool(JsonElement fields, string key) =>
        fields.TryGetProperty(key, out var value) && value.TryGetProperty("booleanValue", out var b)
            ? b.GetBoolean()
            : false;

    private static string GetString(JsonElement fields, string key) =>
        fields.TryGetProperty(key, out var value) && value.TryGetProperty("stringValue", out var s)
            ? s.GetString() ?? ""
            : "";

private static long GetLong(JsonElement fields, string key) =>
        fields.TryGetProperty(key, out var value) && value.TryGetProperty("integerValue", out var i)
            && long.TryParse(i.GetString(), out var parsed)
            ? parsed
            : 0;

    // ---- Parties ----

    public async Task<Party?> GetPartyAsync(string partyId)
    {
        try
        {
            var json = await _client.GetAsync(Url($"parties/{partyId}"));
            return ParseParty(json);
        }
        catch (FirebaseApiException ex) when (ex.ErrorCode == "NOT_FOUND")
        {
            return null;
        }
    }

    public async Task<Party?> GetUserPartyAsync(string uid)
    {
        var docs = await RunQueryAsync("parties", new[]
        {
            ("members", "ARRAY_CONTAINS", (object)uid)
        }, 1);
        return docs.Count > 0 ? ParseParty(docs[0]) : null;
    }

    public async Task<string> CreatePartyAsync(string leaderUid, string leaderDisplayName, string leaderUsername, string leaderAvatar)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var partyId = Guid.NewGuid().ToString("N")[..20];

        var member = new Dictionary<string, object>
        {
            ["mapValue"] = new Dictionary<string, object>
            {
                ["fields"] = new Dictionary<string, object>
                {
                    ["uid"] = Field("stringValue", leaderUid),
                    ["displayName"] = Field("stringValue", leaderDisplayName),
                    ["username"] = Field("stringValue", leaderUsername),
                    ["avatar"] = Field("stringValue", leaderAvatar),
                    ["isLeader"] = Field("booleanValue", "true"),
                    ["isOnline"] = Field("booleanValue", "true"),
                    ["joinedAt"] = Field("integerValue", now.ToString())
                }
            }
        };

        var body = new
        {
            fields = new Dictionary<string, object>
            {
                ["partyId"] = Field("stringValue", partyId),
                ["leaderUid"] = Field("stringValue", leaderUid),
                ["members"] = new Dictionary<string, object>
                {
                    ["arrayValue"] = new Dictionary<string, object>
                    {
                        ["values"] = new[] { member }
                    }
                },
                ["memberCount"] = Field("integerValue", "1"),
                ["createdAt"] = Field("integerValue", now.ToString()),
                ["updatedAt"] = Field("integerValue", now.ToString()),
                ["status"] = Field("stringValue", "active"),
                ["maxMembers"] = Field("integerValue", "3")
            }
        };

        await _client.PatchAsync(Url($"parties/{partyId}?updateMask.fieldPaths=partyId&updateMask.fieldPaths=leaderUid&updateMask.fieldPaths=members&updateMask.fieldPaths=memberCount&updateMask.fieldPaths=createdAt&updateMask.fieldPaths=updatedAt&updateMask.fieldPaths=status&updateMask.fieldPaths=maxMembers"), body);
        return partyId;
    }

    public async Task UpdatePartyAsync(string partyId, Dictionary<string, object> fields)
    {
        if (fields.Count == 0) return;
        fields["updatedAt"] = Field("integerValue", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        var maskQuery = string.Join("&", fields.Keys.Select(m => $"updateMask.fieldPaths={m}"));
        await _client.PatchAsync(Url($"parties/{partyId}?{maskQuery}"), new { fields });
    }

    public async Task DeletePartyAsync(string partyId) =>
        await _client.DeleteAsync(Url($"parties/{partyId}"));

    public async Task<PartyInvite?> GetPartyInviteAsync(string inviteId)
    {
        try
        {
            var json = await _client.GetAsync(Url($"party_invites/{inviteId}"));
            return ParsePartyInvite(json);
        }
        catch (FirebaseApiException ex) when (ex.ErrorCode == "NOT_FOUND")
        {
            return null;
        }
    }

    public async Task CreatePartyInviteAsync(PartyInvite invite)
    {
        var body = new
        {
            fields = new Dictionary<string, object>
            {
                ["inviteId"] = Field("stringValue", invite.InviteId),
                ["fromUid"] = Field("stringValue", invite.FromUid),
                ["toUid"] = Field("stringValue", invite.ToUid),
                ["partyId"] = Field("stringValue", invite.PartyId),
                ["status"] = Field("stringValue", invite.Status.ToString().ToLowerInvariant()),
                ["createdAt"] = Field("integerValue", invite.CreatedAt.ToString()),
                ["expiresAt"] = Field("integerValue", invite.ExpiresAt.ToString())
            }
        };

        await _client.PatchAsync(Url($"party_invites/{invite.InviteId}?updateMask.fieldPaths=inviteId&updateMask.fieldPaths=fromUid&updateMask.fieldPaths=toUid&updateMask.fieldPaths=partyId&updateMask.fieldPaths=status&updateMask.fieldPaths=createdAt&updateMask.fieldPaths=expiresAt"), body);
    }

    public async Task UpdatePartyInviteAsync(string inviteId, Dictionary<string, object> fields)
    {
        if (fields.Count == 0) return;
        var maskQuery = string.Join("&", fields.Keys.Select(m => $"updateMask.fieldPaths={m}"));
        await _client.PatchAsync(Url($"party_invites/{inviteId}?{maskQuery}"), new { fields });
    }

    public async Task<List<PartyInvite>> QueryPartyInvitesAsync(string toUid, PartyInviteStatus? status = null)
    {
        var filters = new List<(string, string, object)> { ("toUid", "EQUAL", (object)toUid) };
        if (status.HasValue)
        {
            filters.Add(("status", "EQUAL", (object)status.Value.ToString().ToLowerInvariant()));
        }
        var docs = await RunQueryAsync("party_invites", filters);
        return docs.Select(ParsePartyInvite).ToList();
    }

    public Task ListenPartyAsync(string partyId, Action<Party> onChange)
    {
        // Firestore realtime listeners would need WebSocket connection
        // For now, implement polling fallback or use Firestore REST with long-polling
        // This is a simplified implementation - production would use Firestore Watch API
        _ = Task.Run(async () =>
        {
            Party? last = null;
            while (true)
            {
                try
                {
                    var party = await GetPartyAsync(partyId);
                    if (party != null && (last == null || party.UpdatedAt != last.UpdatedAt))
                    {
                        last = party;
                        onChange(party);
                    }
                }
                catch { }
                await Task.Delay(3000);
            }
        });
        return Task.CompletedTask;
    }

    public Task ListenPartyInvitesAsync(string toUid, Action<List<PartyInvite>> onChange)
    {
        _ = Task.Run(async () =>
        {
            List<PartyInvite>? last = null;
            while (true)
            {
                try
                {
                    var invites = await QueryPartyInvitesAsync(toUid, PartyInviteStatus.Pending);
                    if (last == null || invites.Count != last.Count ||
                        invites.Any(i => !last.Any(l => l.InviteId == i.InviteId)))
                    {
                        last = invites;
                        onChange(invites);
                    }
                }
                catch { }
                await Task.Delay(3000);
            }
        });
        return Task.CompletedTask;
    }

    public Task StopListenPartyAsync(string partyId) => Task.CompletedTask;
    public Task StopListenPartyInvitesAsync(string toUid) => Task.CompletedTask;

    private static Party ParseParty(JsonElement json)
    {
        var party = new Party();
        if (!json.TryGetProperty("fields", out var fields)) return party;

        party.PartyId = GetString(fields, "partyId");
        party.LeaderUid = GetString(fields, "leaderUid");
        party.CreatedAt = GetLong(fields, "createdAt");
        party.UpdatedAt = GetLong(fields, "updatedAt");
        party.Status = Enum.TryParse<PartyStatus>(GetString(fields, "status"), true, out var s) ? s : PartyStatus.Active;
        party.MaxMembers = (int)GetLong(fields, "maxMembers");

        if (fields.TryGetProperty("members", out var members) && members.TryGetProperty("arrayValue", out var array) &&
            array.TryGetProperty("values", out var values))
        {
            foreach (var value in values.EnumerateArray())
            {
                if (value.TryGetProperty("mapValue", out var mv) && mv.TryGetProperty("fields", out var mf))
                {
                    party.Members.Add(ParsePartyMember(mf, useVoiceJoinedAt: false));
                }
            }
        }

        if (fields.TryGetProperty("participants", out var participants) && participants.TryGetProperty("arrayValue", out var pArray) &&
            pArray.TryGetProperty("values", out var pValues))
        {
            foreach (var value in pValues.EnumerateArray())
            {
                if (value.TryGetProperty("mapValue", out var mv) && mv.TryGetProperty("fields", out var mf))
                {
                    party.Members.Add(ParsePartyMember(mf, useVoiceJoinedAt: true));
                }
            }
        }

        return party;
    }

    private static PartyMember ParsePartyMember(JsonElement mf, bool useVoiceJoinedAt)
    {
        var joinedAt = useVoiceJoinedAt ? GetLong(mf, "joinedVoiceAt") : GetLong(mf, "joinedAt");
        if (joinedAt == 0) joinedAt = GetLong(mf, "joinedAt");

        return new PartyMember
        {
            Uid = GetString(mf, "uid"),
            DisplayName = GetString(mf, "displayName"),
            Username = GetString(mf, "username"),
            Avatar = GetString(mf, "avatar"),
            IsLeader = GetBool(mf, "isLeader"),
            IsOnline = GetBool(mf, "isOnline"),
            JoinedAt = DateTimeOffset.FromUnixTimeMilliseconds(joinedAt),
            IsInVoice = GetBool(mf, "isInVoice"),
            IsVoiceMuted = GetBool(mf, "isVoiceMuted"),
            IsVoiceDeafened = GetBool(mf, "isVoiceDeafened"),
            IsSpeaking = GetBool(mf, "isSpeaking")
        };
    }

    private static PartyInvite ParsePartyInvite(JsonElement json)
    {
        var invite = new PartyInvite();
        if (!json.TryGetProperty("fields", out var fields)) return invite;

        invite.InviteId = GetString(fields, "inviteId");
        invite.FromUid = GetString(fields, "fromUid");
        invite.ToUid = GetString(fields, "toUid");
        invite.PartyId = GetString(fields, "partyId");
        invite.Status = Enum.TryParse<PartyInviteStatus>(GetString(fields, "status"), true, out var s) ? s : PartyInviteStatus.Pending;
        invite.CreatedAt = GetLong(fields, "createdAt");
        invite.ExpiresAt = GetLong(fields, "expiresAt");
        return invite;
    }

    // ---- Voice Sessions ----

    public async Task<Party?> GetVoiceSessionAsync(string partyId)
    {
        try
        {
            var json = await _client.GetAsync(Url($"voice_sessions/{partyId}"));
            return ParseParty(json);
        }
        catch (FirebaseApiException ex) when (ex.ErrorCode == "NOT_FOUND")
        {
            return null;
        }
    }

    public async Task UpdateVoiceSessionAsync(string partyId, Dictionary<string, object> fields)
    {
        if (fields.Count == 0) return;
        fields["updatedAt"] = Field("integerValue", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        var maskQuery = string.Join("&", fields.Keys.Select(m => $"updateMask.fieldPaths={m}"));
        await _client.PatchAsync(Url($"voice_sessions/{partyId}?{maskQuery}"), new { fields });
    }

    public async Task DeleteVoiceSessionAsync(string partyId) =>
        await _client.DeleteAsync(Url($"voice_sessions/{partyId}"));

    public Task ListenVoiceSessionAsync(string partyId, Action<Party> onChange)
    {
        _ = Task.Run(async () =>
        {
            Party? last = null;
            while (true)
            {
                try
                {
                    var party = await GetVoiceSessionAsync(partyId);
                    if (party != null && (last == null || party.UpdatedAt != last.UpdatedAt))
                    {
                        last = party;
                        onChange(party);
                    }
                }
                catch { }
                await Task.Delay(3000);
            }
        });
        return Task.CompletedTask;
    }

    // ---- Voice Signaling ----

    public async Task<VoiceSignalDoc?> GetVoiceSignalAsync(string partyId, string signalDocId)
    {
        try
        {
            var json = await _client.GetAsync(Url($"voice_sessions/{partyId}/signals/{signalDocId}"));
            return ParseVoiceSignal(signalDocId, json);
        }
        catch (FirebaseApiException ex) when (ex.ErrorCode == "NOT_FOUND")
        {
            return null;
        }
    }

    public async Task UpdateVoiceSignalAsync(string partyId, string signalDocId, Dictionary<string, object> fields)
    {
        if (fields.Count == 0) return;
        fields["updatedAt"] = Field("integerValue", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        var maskQuery = string.Join("&", fields.Keys.Select(m => $"updateMask.fieldPaths={m}"));
        await _client.PatchAsync(Url($"voice_sessions/{partyId}/signals/{signalDocId}?{maskQuery}"), new { fields });
    }

    public async Task DeleteVoiceSignalAsync(string partyId, string signalDocId) =>
        await _client.DeleteAsync(Url($"voice_sessions/{partyId}/signals/{signalDocId}"));

    public Task ListenVoiceSignalAsync(string partyId, string signalDocId, Action<VoiceSignalDoc> onChange)
    {
        _ = Task.Run(async () =>
        {
            VoiceSignalDoc? last = null;
            while (true)
            {
                try
                {
                    var signal = await GetVoiceSignalAsync(partyId, signalDocId);
                    if (signal != null && (last == null || signal.UpdatedAt != last.UpdatedAt))
                    {
                        last = signal;
                        onChange(signal);
                    }
                }
                catch { }
                await Task.Delay(1500);
            }
        });
        return Task.CompletedTask;
    }

    private static VoiceSignalDoc ParseVoiceSignal(string signalDocId, JsonElement json)
    {
        var signal = new VoiceSignalDoc { SignalId = signalDocId };
        if (!json.TryGetProperty("fields", out var fields)) return signal;

        signal.PartyId = GetString(fields, "partyId");
        signal.FromUid = GetString(fields, "fromUid");
        signal.ToUid = GetString(fields, "toUid");
        signal.Offer = GetString(fields, "offer");
        signal.Answer = GetString(fields, "answer");
        signal.UpdatedAt = GetLong(fields, "updatedAt");

        if (fields.TryGetProperty("offererCandidates", out var offererCands) &&
            offererCands.TryGetProperty("arrayValue", out var oa) &&
            oa.TryGetProperty("values", out var ov))
        {
            foreach (var v in ov.EnumerateArray())
            {
                if (v.TryGetProperty("stringValue", out var sv) && sv.ValueKind == JsonValueKind.String)
                {
                    signal.OffererCandidates.Add(sv.GetString() ?? "");
                }
            }
        }

        if (fields.TryGetProperty("answererCandidates", out var answererCands) &&
            answererCands.TryGetProperty("arrayValue", out var aa) &&
            aa.TryGetProperty("values", out var av))
        {
            foreach (var v in av.EnumerateArray())
            {
                if (v.TryGetProperty("stringValue", out var sv) && sv.ValueKind == JsonValueKind.String)
                {
                    signal.AnswererCandidates.Add(sv.GetString() ?? "");
                }
            }
        }

        return signal;
    }
}
