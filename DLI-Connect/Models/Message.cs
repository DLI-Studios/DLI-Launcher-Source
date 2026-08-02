namespace DLI.Connect.Models;

public class Message
{
    public string MessageId { get; set; } = "";
    public string SenderUid { get; set; } = "";
    public string Text { get; set; } = "";
    public long CreatedAt { get; set; }
    public bool Read { get; set; }
    public long ReadAt { get; set; }
    public bool Deleted { get; set; }
    public long DeletedAt { get; set; }
}
