using DLI.Connect.Firebase;
using DLI.Connect.Models;

namespace DLI.Connect.Services.Interfaces;

public interface ISessionManager
{
    FirebaseUser? CurrentUser { get; }
    UserProfile? Profile { get; }
    event Action? StateChanged;
    bool IsLoggedIn { get; }
    bool IsEmailVerified { get; }

    Task<FirebaseUser> LoginAsync(string email, string password, bool remember);
    Task<bool> TryRestoreSessionAsync();
    Task LogoutAsync();
    Task RefreshProfileAsync();
    Task SetOnlineAsync();
    Task SetOfflineAsync();
    Task SetStatusAsync(string status);
    Task UpdateProfileAsync(string? displayName = null, string? bio = null);
    Task UpdateAvatarAsync(byte[] imageBytes);
    Task RemoveAvatarAsync();
    Task UpdateSettingsAsync(string? theme = null, UserPrivacy? privacy = null, UserNotifications? notifications = null);
    Task ChangePasswordAsync(string currentPassword, string newPassword);
    Task DeleteAccountAsync();
    Task RegisterAsync(string username, string displayName, string email, string password);
    Task ResendVerificationAsync();
    Task<FirebaseUser> RefreshVerificationStateAsync();
    Task ForgotPasswordAsync(string email);
    Task ShutdownAsync();
}