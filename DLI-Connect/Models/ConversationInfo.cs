namespace DLI.Connect.Models;

public class ConversationInfo
{
    public string ConversationId { get; set; } = "";
    public string ParticipantA { get; set; } = "";
    public string ParticipantB { get; set; } = "";
    public List<string> Participants { get; set; } = new();
    public long UnreadA { get; set; }
    public long UnreadB { get; set; }
    public bool HiddenA { get; set; }
    public bool HiddenB { get; set; }
    public long HiddenUntilA { get; set; }
    public long HiddenUntilB { get; set; }
    public string LastMessage { get; set; } = "";
    public long LastMessageTime { get; set; }
    public string LastSenderUid { get; set; } = "";
    public long CreatedAt { get; set; }

    public bool IsParticipantA(string me) => ParticipantA == me;
    public string PeerUid(string me) => IsParticipantA(me) ? ParticipantB : ParticipantA;
    public long MyUnread(string me) => IsParticipantA(me) ? UnreadA : UnreadB;
}
