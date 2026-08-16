using System;

namespace DLI.Connect.Models;

public enum UserStatus
{
    Offline,
    Online,
    Away,
    DoNotDisturb,
    Invisible
}

public class UserPrivacy
{
    public string FriendRequests { get; set; } = "everyone"; // everyone | friends | nobody
    public bool ShowStatus { get; set; } = true;
    public bool ShowActivity { get; set; } = true;
}

public class UserNotifications
{
    public bool Enabled { get; set; } = true;
    public bool FriendRequests { get; set; } = true;
    public bool Messages { get; set; } = true;
    public bool PartyInvites { get; set; } = true;
}

public class UserProfile
{
    public string Uid { get; set; } = "";
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Avatar { get; set; } = "";
    public string Bio { get; set; } = "";
    public string Status { get; set; } = Presence.Offline;
    public string Theme { get; set; } = "dark"; // dark | light | system
    public UserPrivacy Privacy { get; set; } = new();
    public UserNotifications Notifications { get; set; } = new();
    public long CreatedAt { get; set; }
    public long LastSeen { get; set; }

    public UserStatus StatusEnum => Status switch
    {
        Presence.Online => UserStatus.Online,
        Presence.Away => UserStatus.Away,
        Presence.DoNotDisturb => UserStatus.DoNotDisturb,
        Presence.Invisible => UserStatus.Invisible,
        _ => UserStatus.Offline
    };

    public bool IsPresent => Presence.IsPresent(Status, LastSeen);
}

public static class Presence
{
    public const string Online = "online";
    public const string Away = "away";
    public const string DoNotDisturb = "dnd";
    public const string Invisible = "invisible";
    public const string Offline = "offline";

    public static bool IsPresent(string status, long lastSeen) =>
        status is Online or Away or DoNotDisturb &&
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastSeen < 90_000;
}
