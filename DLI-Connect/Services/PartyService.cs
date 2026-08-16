using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.Services;

public class PartyService : IPartyService
{
    private readonly IFirebaseFirestore _firestore;
    private readonly ISessionManager _session;
    private Party? _cachedParty;
    private readonly object _lock = new();
    private IDisposable? _partyListener;
    private IDisposable? _inviteListener;

    public PartyService(IFirebaseFirestore firestore, ISessionManager session)
    {
        _firestore = firestore;
        _session = session;
        _session.StateChanged += OnSessionChanged;
    }

    private void OnSessionChanged()
    {
        lock (_lock)
        {
            _cachedParty = null;
            StopListeners();
        }
    }

    private void StopListeners()
    {
        _partyListener?.Dispose();
        _inviteListener?.Dispose();
        _partyListener = null;
        _inviteListener = null;
    }

    public async Task<Party?> GetCurrentPartyAsync()
    {
        var uid = _session.CurrentUser?.Uid;
        if (string.IsNullOrEmpty(uid)) return null;

        lock (_lock)
        {
            if (_cachedParty != null) return _cachedParty;
        }

        var party = await _firestore.GetUserPartyAsync(uid);
        if (party != null)
        {
            lock (_lock)
            {
                _cachedParty = party;
                StartPartyListener(party.PartyId);
                StartInviteListener(uid);
            }
        }
        return party;
    }

    private void StartPartyListener(string partyId)
    {
        if (_partyListener != null) return;
        _firestore.ListenPartyAsync(partyId, party =>
        {
            lock (_lock)
            {
                _cachedParty = party;
            }
        });
    }

    private void StartInviteListener(string uid)
    {
        if (_inviteListener != null) return;
        _firestore.ListenPartyInvitesAsync(uid, invites =>
        {
            // Invites updated - could notify UI via event
        });
    }

    public async Task<Party?> CreatePartyAsync()
    {
        var uid = _session.CurrentUser?.Uid;
        var profile = _session.Profile;
        if (string.IsNullOrEmpty(uid) || profile == null) return null;

        if (await IsInPartyAsync()) return null;

        var partyId = await _firestore.CreatePartyAsync(uid, profile.DisplayName, profile.Username, profile.Avatar);
        if (partyId == null) return null;

        var party = await _firestore.GetPartyAsync(partyId);
        if (party != null)
        {
            lock (_lock)
            {
                _cachedParty = party;
                StartPartyListener(partyId);
                StartInviteListener(uid);
            }
        }
        return party;
    }

    public async Task LeavePartyAsync()
    {
        var uid = _session.CurrentUser?.Uid;
        var party = await GetCurrentPartyAsync();
        if (party == null || uid == null) return;

        var member = party.Members.FirstOrDefault(m => m.Uid == uid);
        if (member == null) return;

        var isLeader = member.IsLeader;
        var remaining = party.Members.Where(m => m.Uid != uid).ToList();

        if (isLeader)
        {
            if (remaining.Count == 0)
            {
                await DisbandPartyAsync();
                return;
            }
            // Transfer leadership to first remaining member
            var newLeader = remaining[0];
            remaining[0] = new PartyMember
            {
                Uid = newLeader.Uid,
                DisplayName = newLeader.DisplayName,
                Username = newLeader.Username,
                Avatar = newLeader.Avatar,
                IsLeader = true,
                IsOnline = newLeader.IsOnline,
                JoinedAt = newLeader.JoinedAt
            };
        }

        var updates = new Dictionary<string, object>
        {
            ["members"] = BuildMembersArray(remaining),
            ["memberCount"] = remaining.Count,
            ["leaderUid"] = isLeader ? remaining[0].Uid : party.LeaderUid
        };

        await _firestore.UpdatePartyAsync(party.PartyId, updates);
        lock (_lock)
        {
            _cachedParty = null;
            StopListeners();
        }
    }

    public async Task DisbandPartyAsync()
    {
        var party = await GetCurrentPartyAsync();
        if (party == null) return;

        // Delete all pending invites for this party
        var invites = await _firestore.QueryPartyInvitesAsync(party.PartyId);
        foreach (var invite in invites)
        {
            if (invite.PartyId == party.PartyId)
            {
                await _firestore.UpdatePartyInviteAsync(invite.InviteId, new Dictionary<string, object>
                {
                    ["status"] = Field("stringValue", "expired")
                });
            }
        }

        await _firestore.DeletePartyAsync(party.PartyId);
        lock (_lock)
        {
            _cachedParty = null;
            StopListeners();
        }
    }

    public async Task<string?> InviteFriendAsync(string friendUid)
    {
        var uid = _session.CurrentUser?.Uid;
        var party = await GetCurrentPartyAsync();
        Log($"InviteFriendAsync friendUid='{friendUid}' uid='{uid}' party={party?.PartyId} members={party?.Members.Count} max={party?.MaxMembers} leaderUid={party?.LeaderUid}");
        if (party == null || uid == null) { Log("Invite: early return (party null or uid null)"); return null; }

        if (!party.Members.Any(m => m.Uid == uid && m.IsLeader)) { Log("Invite: early return (not leader)"); return "Partide yönetici değilsin."; }
        if (party.Members.Count >= party.MaxMembers) { Log("Invite: early return (full)"); return "Parti dolu."; }
        if (party.Members.Any(m => m.Uid == friendUid)) { Log("Invite: early return (already member)"); return "Arkadaş zaten partide."; }

        // Check if friend is already in a party
        var friendParty = await _firestore.GetUserPartyAsync(friendUid);
        if (friendParty != null) { Log($"Invite: early return (friend in party {friendParty.PartyId})"); return "Arkadaş başka bir partide."; }

        // Check if already invited
        var existing = await _firestore.GetPartyInviteAsync($"{party.PartyId}_{friendUid}");
        if (existing != null && existing.Status == PartyInviteStatus.Pending) { Log("Invite: early return (already invited pending)"); return "Bu arkadaşa zaten davet gönderildi."; }

        var invite = new PartyInvite
        {
            InviteId = $"{party.PartyId}_{friendUid}",
            FromUid = uid,
            ToUid = friendUid,
            PartyId = party.PartyId,
            Status = PartyInviteStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds()
        };

        await _firestore.CreatePartyInviteAsync(invite);
        Log($"Invite: created {invite.InviteId}");
        return null;
    }

    private static void Log(string message)
    {
        try
        {
            System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dli-connect.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [PartyService] {message}\n");
        }
        catch { }
    }

    public async Task AcceptInviteAsync(string inviteId)
    {
        var uid = _session.CurrentUser?.Uid;
        if (uid == null) return;

        var invite = await _firestore.GetPartyInviteAsync(inviteId);
        if (invite == null || invite.ToUid != uid || invite.Status != PartyInviteStatus.Pending) return;

        var party = await _firestore.GetPartyAsync(invite.PartyId);
        if (party == null || party.MemberCount >= party.MaxMembers) return;

        // Check if user already in a party
        var userParty = await _firestore.GetUserPartyAsync(uid);
        if (userParty != null) return;

        var profile = _session.Profile;
        var newMember = new Dictionary<string, object>
        {
            ["mapValue"] = new Dictionary<string, object>
            {
                ["fields"] = new Dictionary<string, object>
                {
                    ["uid"] = Field("stringValue", uid),
                    ["displayName"] = Field("stringValue", profile?.DisplayName ?? ""),
                    ["username"] = Field("stringValue", profile?.Username ?? ""),
                    ["avatar"] = Field("stringValue", profile?.Avatar ?? ""),
                    ["isLeader"] = Field("booleanValue", "false"),
                    ["isOnline"] = Field("booleanValue", "true"),
                    ["joinedAt"] = Field("integerValue", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString())
                }
            }
        };

        // Add member to party
        var updatedMembers = party.Members.ToList();
        updatedMembers.Add(new PartyMember
        {
            Uid = uid,
            DisplayName = profile?.DisplayName ?? "",
            Username = profile?.Username ?? "",
            Avatar = profile?.Avatar ?? "",
            IsLeader = false,
            IsOnline = true,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await _firestore.UpdatePartyAsync(invite.PartyId, new Dictionary<string, object>
        {
            ["members"] = BuildMembersArray(updatedMembers),
            ["memberCount"] = updatedMembers.Count
        });

        // Update invite status
        await _firestore.UpdatePartyInviteAsync(inviteId, new Dictionary<string, object>
        {
            ["status"] = Field("stringValue", "accepted")
        });

        lock (_lock) { _cachedParty = null; }
    }

    public async Task DeclineInviteAsync(string inviteId)
    {
        var uid = _session.CurrentUser?.Uid;
        if (uid == null) return;

        var invite = await _firestore.GetPartyInviteAsync(inviteId);
        if (invite == null || invite.ToUid != uid || invite.Status != PartyInviteStatus.Pending) return;

        await _firestore.UpdatePartyInviteAsync(inviteId, new Dictionary<string, object>
        {
            ["status"] = Field("stringValue", "declined")
        });
    }

    public async Task CancelInviteAsync(string inviteId)
    {
        var uid = _session.CurrentUser?.Uid;
        var party = await GetCurrentPartyAsync();
        if (party == null || uid == null) return;

        if (!party.Members.Any(m => m.Uid == uid && m.IsLeader)) return;

        var invite = await _firestore.GetPartyInviteAsync(inviteId);
        if (invite == null || invite.PartyId != party.PartyId) return;

        await _firestore.UpdatePartyInviteAsync(inviteId, new Dictionary<string, object>
        {
            ["status"] = Field("stringValue", "expired")
        });
    }

    public async Task KickMemberAsync(string memberUid)
    {
        var uid = _session.CurrentUser?.Uid;
        var party = await GetCurrentPartyAsync();
        if (party == null || uid == null) return;

        if (!party.Members.Any(m => m.Uid == uid && m.IsLeader)) return;
        if (memberUid == uid) return; // Cannot kick self

        var remaining = party.Members.Where(m => m.Uid != memberUid).ToList();

        await _firestore.UpdatePartyAsync(party.PartyId, new Dictionary<string, object>
        {
            ["members"] = BuildMembersArray(remaining),
            ["memberCount"] = remaining.Count
        });

        lock (_lock) { _cachedParty = null; }
    }

    public async Task TransferLeadershipAsync(string memberUid)
    {
        var uid = _session.CurrentUser?.Uid;
        var party = await GetCurrentPartyAsync();
        if (party == null || uid == null) return;

        if (!party.Members.Any(m => m.Uid == uid && m.IsLeader)) return;
        if (!party.Members.Any(m => m.Uid == memberUid)) return;

        var updates = new Dictionary<string, object>
        {
            ["leaderUid"] = memberUid,
            ["members"] = BuildMembersArray(party.Members.Select(m => m.Uid == memberUid
                ? new PartyMember { Uid = m.Uid, DisplayName = m.DisplayName, Username = m.Username, Avatar = m.Avatar, IsLeader = true, IsOnline = m.IsOnline, JoinedAt = m.JoinedAt }
                : m.Uid == uid
                    ? new PartyMember { Uid = m.Uid, DisplayName = m.DisplayName, Username = m.Username, Avatar = m.Avatar, IsLeader = false, IsOnline = m.IsOnline, JoinedAt = m.JoinedAt }
                    : m).ToList())
        };

        await _firestore.UpdatePartyAsync(party.PartyId, updates);
        lock (_lock) { _cachedParty = null; }
    }

    public async Task<List<PartyInvite>> GetPendingInvitesAsync()
    {
        var uid = _session.CurrentUser?.Uid;
        if (string.IsNullOrEmpty(uid)) return new List<PartyInvite>();
        return await _firestore.QueryPartyInvitesAsync(uid, PartyInviteStatus.Pending);
    }

    public async Task<bool> IsInPartyAsync()
    {
        var party = await GetCurrentPartyAsync();
        return party != null;
    }

    public async Task<int> GetPartyMemberCountAsync()
    {
        var party = await GetCurrentPartyAsync();
        return party?.MemberCount ?? 0;
    }

    private static Dictionary<string, object> Field(string type, string value) =>
        new() { [type] = value };

    private static object BuildMembersArray(List<PartyMember> members)
    {
        var arr = new
        {
            arrayValue = new Dictionary<string, object>
            {
                ["values"] = members.Select(m => new Dictionary<string, object>
                {
                    ["mapValue"] = new Dictionary<string, object>
                    {
                        ["fields"] = new Dictionary<string, object>
                        {
                            ["uid"] = Field("stringValue", m.Uid),
                            ["displayName"] = Field("stringValue", m.DisplayName),
                            ["username"] = Field("stringValue", m.Username),
                            ["avatar"] = Field("stringValue", m.Avatar),
                            ["isLeader"] = Field("booleanValue", m.IsLeader ? "true" : "false"),
                            ["isOnline"] = Field("booleanValue", m.IsOnline ? "true" : "false"),
                            ["joinedAt"] = Field("integerValue", m.JoinedAt.ToUnixTimeMilliseconds().ToString())
                        }
                    }
                }).ToArray()
            }
        };
        return arr;
    }
}