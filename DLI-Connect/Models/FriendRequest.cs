namespace DLI.Connect.Models;

public class FriendRequest
{
    public string RequestId { get; set; } = "";
    public string FromUid { get; set; } = "";
    public string ToUid { get; set; } = "";
    public string Status { get; set; } = "pending";
    public long CreatedAt { get; set; }
}
