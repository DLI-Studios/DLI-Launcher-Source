using DLI.Connect.Models;

namespace DLI.Connect.Services.Interfaces;

public interface IFirebaseAuth
{
    Task<FirebaseUser> SignInWithPasswordAsync(string email, string password);
    Task<FirebaseUser> SignUpAsync(string email, string password);
    Task<FirebaseUser> RefreshTokenAsync(string refreshToken);
    Task<FirebaseUser> GetUserInfoAsync(string idToken);
    Task SendVerificationEmailAsync(string idToken);
    Task SendPasswordResetEmailAsync(string email);
    Task ChangePasswordAsync(string idToken, string newPassword);
    Task DeleteAccountAsync(string idToken);
}