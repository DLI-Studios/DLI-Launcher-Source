using System;
using System.Threading.Tasks;
using DLI.Connect.Firebase;
using DLI.Connect.Helpers;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.Services;

public class SessionManager : ISessionManager
{
    private readonly IFirebaseAuth _auth;
    private readonly IFirebaseFirestore _firestore;
    private readonly IFirebaseStorage _storage;
    private System.Threading.Timer? _heartbeat;
    private string _manualStatus = Presence.Online;

    public FirebaseUser? CurrentUser { get; private set; }
    public UserProfile? Profile { get; private set; }
    public event Action? StateChanged;

    public bool IsLoggedIn => CurrentUser != null;
    public bool IsEmailVerified => CurrentUser?.EmailVerified ?? false;

    public SessionManager(IFirebaseAuth auth, IFirebaseFirestore firestore, IFirebaseStorage storage)
    {
        _auth = auth;
        _firestore = firestore;
        _storage = storage;
    }

    private void StartHeartbeat()
    {
        _heartbeat?.Dispose();
        _heartbeat = new System.Threading.Timer(
            _ => _ = SetStatusAsync(_manualStatus),
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
    }

    private void StopHeartbeat()
    {
        _heartbeat?.Dispose();
        _heartbeat = null;
    }

    public async Task<FirebaseUser> LoginAsync(string email, string password, bool remember)
    {
        var user = await _auth.SignInWithPasswordAsync(email, password);

        // Always fetch fresh verification state
        user = await _auth.GetUserInfoAsync(user.IdToken);

        if (user.EmailVerified)
        {
            CurrentUser = user;
            await RefreshProfileAsync();
            await SetOnlineAsync();
            StartHeartbeat();

            if (remember)
            {
                SessionStorage.SaveRefreshToken(user.RefreshToken);
            }
            else
            {
                SessionStorage.Clear();
            }
        }
        else
        {
            // Keep a partial session so the VerifyEmail page can resend the link.
            CurrentUser = user;
            SessionStorage.Clear();
        }

        StateChanged?.Invoke();
        return user;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        var refreshToken = SessionStorage.LoadRefreshToken();
        if (string.IsNullOrEmpty(refreshToken)) return false;

        try
        {
            var user = await _auth.RefreshTokenAsync(refreshToken);
            user = await _auth.GetUserInfoAsync(user.IdToken);

            if (!user.EmailVerified) return false;

            CurrentUser = user;
            SessionStorage.SaveRefreshToken(user.RefreshToken);
            await RefreshProfileAsync();
            await SetOnlineAsync();
            StartHeartbeat();
            StateChanged?.Invoke();
            return true;
        }
        catch
        {
            SessionStorage.Clear();
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        StopHeartbeat();
        if (CurrentUser != null)
        {
            try { await SetOfflineAsync(); } catch { }
        }
        CurrentUser = null;
        Profile = null;
        SessionStorage.Clear();
        StateChanged?.Invoke();
    }

    public async Task RefreshProfileAsync()
    {
        if (CurrentUser == null) return;

        Profile = await _firestore.GetUserAsync(CurrentUser.Uid);

        if (Profile == null)
        {
            var fallback = CurrentUser.Email.Contains('@') ? CurrentUser.Email[..CurrentUser.Email.IndexOf('@')] : CurrentUser.Uid;
            try
            {
                await _firestore.CreateUserAsync(CurrentUser.Uid, fallback, fallback, CurrentUser.Email);
                Profile = await _firestore.GetUserAsync(CurrentUser.Uid);
            }
            catch
            {
                // Keep Profile null; HomeView falls back to session data.
            }
        }

        StateChanged?.Invoke();
    }

    public async Task SetOnlineAsync()
    {
        _manualStatus = Presence.Online;
        if (CurrentUser == null) return;
        try
        {
            await _firestore.UpdateStatusAsync(CurrentUser.Uid, Presence.Online);
            if (Profile != null) Profile.Status = Presence.Online;
        }
        catch { }
    }

    public async Task SetOfflineAsync()
    {
        _manualStatus = Presence.Offline;
        if (CurrentUser == null) return;
        try
        {
            await _firestore.UpdateStatusAsync(CurrentUser.Uid, Presence.Offline);
            if (Profile != null) Profile.Status = Presence.Offline;
        }
        catch { }
    }

    public async Task SetStatusAsync(string status)
    {
        _manualStatus = status;
        if (CurrentUser == null) return;
        try
        {
            await _firestore.UpdateStatusAsync(CurrentUser.Uid, status);
            if (Profile != null) Profile.Status = status;
            StateChanged?.Invoke();
        }
        catch { }
    }

    public async Task UpdateProfileAsync(string? displayName = null, string? bio = null)
    {
        if (CurrentUser == null) return;
        await _firestore.UpdateProfileAsync(CurrentUser.Uid, displayName, null, bio);
        await RefreshProfileAsync();
    }

    public async Task UpdateAvatarAsync(byte[] imageBytes)
    {
        if (CurrentUser == null) return;
        var url = await _storage.UploadAvatarAsync(imageBytes, CurrentUser.Uid);
        await _firestore.UpdateProfileAsync(CurrentUser.Uid, avatar: url);
        await RefreshProfileAsync();
    }

    public async Task RemoveAvatarAsync()
    {
        if (CurrentUser == null) return;
        try { await _storage.DeleteAvatarAsync(CurrentUser.Uid); } catch { }
        await _firestore.UpdateProfileAsync(CurrentUser.Uid, avatar: "");
        await RefreshProfileAsync();
    }

    public async Task UpdateSettingsAsync(string? theme = null, UserPrivacy? privacy = null, UserNotifications? notifications = null)
    {
        if (CurrentUser == null) return;
        await _firestore.UpdateSettingsAsync(CurrentUser.Uid, theme, privacy, notifications);
        await RefreshProfileAsync();
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (CurrentUser == null) throw new InvalidOperationException("Oturum açık değil.");

        // Verify the current password before allowing a change.
        await _auth.SignInWithPasswordAsync(CurrentUser.Email, currentPassword);
        await _auth.ChangePasswordAsync(CurrentUser.IdToken, newPassword);
    }

    public async Task DeleteAccountAsync()
    {
        if (CurrentUser == null) throw new InvalidOperationException("Oturum açık değil.");

        StopHeartbeat();
        var uid = CurrentUser.Uid;

        try { await _firestore.DeleteDocumentAsync($"users/{uid}"); } catch { }
        try { await _storage.DeleteAvatarAsync(uid); } catch { }
        try { await _auth.DeleteAccountAsync(CurrentUser.IdToken); } catch { }

        CurrentUser = null;
        Profile = null;
        SessionStorage.Clear();
        StateChanged?.Invoke();
    }

    public async Task RegisterAsync(string username, string displayName, string email, string password)
    {
        var user = await _auth.SignUpAsync(email, password);

        CurrentUser = user;

        try
        {
            await _firestore.CreateUserAsync(user.Uid, username, displayName, email);
        }
        catch
        {
            // Profile is optional at this stage; it gets healed on first login.
        }

        await _auth.SendVerificationEmailAsync(user.IdToken);
    }

    public async Task ResendVerificationAsync()
    {
        if (CurrentUser == null) return;
        await _auth.SendVerificationEmailAsync(CurrentUser.IdToken);
    }

    public async Task<FirebaseUser> RefreshVerificationStateAsync()
    {
        if (CurrentUser == null)
        {
            throw new InvalidOperationException("Kullanıcı oturumu yok.");
        }

        var fresh = await _auth.GetUserInfoAsync(CurrentUser.IdToken);
        if (fresh.EmailVerified)
        {
            CurrentUser = fresh;
            await RefreshProfileAsync();
            await SetOnlineAsync();
            StartHeartbeat();
            StateChanged?.Invoke();
        }
        return fresh;
    }

    public async Task ForgotPasswordAsync(string email) =>
        await _auth.SendPasswordResetEmailAsync(email);

    public async Task ShutdownAsync()
    {
        StopHeartbeat();
        var offlineTask = SetOfflineAsync();
        await Task.WhenAny(offlineTask, Task.Delay(1500));
        CurrentUser = null;
        Profile = null;
    }
}
