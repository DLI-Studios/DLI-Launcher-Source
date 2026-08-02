namespace DLI.Connect.Models;

public class FirebaseUser
{
    public string IdToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string Uid { get; set; } = "";
    public string Email { get; set; } = "";
    public bool EmailVerified { get; set; }
    public string DisplayName { get; set; } = "";
    public string ExpiresInSeconds { get; set; } = "";
}
