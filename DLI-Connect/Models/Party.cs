using System;

namespace DLI.Connect.Models;

public enum PartyStatus
{
    Active,
    Disbanded
}

public enum PartyInviteStatus
{
    Pending,
    Accepted,
    Declined,
    Expired
}

public class PartyMember
{
    public string Uid { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Avatar { get; set; } = "";
    public bool IsLeader { get; set; }
    public bool IsOnline { get; set; }
    public DateTimeOffset JoinedAt { get; set; }

    public bool IsInVoice { get; set; }
    public bool IsVoiceMuted { get; set; }
    public bool IsVoiceDeafened { get; set; }
    public bool IsSpeaking { get; set; }
}

public class Party
{
    public string PartyId { get; set; } = "";
    public string LeaderUid { get; set; } = "";
    public List<PartyMember> Members { get; set; } = new();
    public int MemberCount => Members.Count;
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public PartyStatus Status { get; set; } = PartyStatus.Active;
    public int MaxMembers { get; set; } = 3;
}

public class PartyInvite
{
    public string InviteId { get; set; } = "";
    public string FromUid { get; set; } = "";
    public string ToUid { get; set; } = "";
    public string PartyId { get; set; } = "";
    public PartyInviteStatus Status { get; set; } = PartyInviteStatus.Pending;
    public long CreatedAt { get; set; }
    public long ExpiresAt { get; set; }
}