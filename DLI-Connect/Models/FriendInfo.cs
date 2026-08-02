using System;

namespace DLI.Connect.Models;

public class FriendInfo
{
    public string Uid { get; set; } = "";
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Avatar { get; set; } = "";
    public string Status { get; set; } = Presence.Offline;
    public long LastSeen { get; set; }
    public bool HidePresence { get; set; }

    public bool IsOnline => !HidePresence && Presence.IsPresent(Status, LastSeen);
    public bool IsAway => !HidePresence && Status == Presence.Away && IsRecent;
    public bool IsDoNotDisturb => !HidePresence && Status == Presence.DoNotDisturb && IsRecent;
    public bool IsInvisible => !HidePresence && Status == Presence.Invisible;

    private bool IsRecent => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LastSeen < 90_000;

    public string StatusText => !IsOnline
        ? "Çevrimdışı"
        : Status switch
        {
            Presence.Away => "Boşta",
            Presence.DoNotDisturb => "Rahatsız Etmeyin",
            Presence.Invisible => "Gizli",
            _ => "Çevrimiçi"
        };

    public string StatusColorHex => IsDoNotDisturb ? "#F23F43" : IsAway ? "#F0B232" : "#23A55A";

    public string Initial => string.IsNullOrWhiteSpace(DisplayName) ? "?" : DisplayName.Trim()[..1].ToUpperInvariant();
}

public enum RequestRelationState
{
    None,
    RequestSent,
    AlreadyFriends,
    IncomingRequest
}
