using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using DLI.Connect.Models;

using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.Firebase;

public class FirebaseAuth : IFirebaseAuth
{
    private readonly IFirebaseClient _client;

    public FirebaseAuth(IFirebaseClient client)
    {
        _client = client;
    }

    private string AuthUrl(string endpoint) =>
        $"{FirebaseConfig.AuthBaseUrl}/{endpoint}?key={FirebaseConfig.ApiKey}";

    public async Task<FirebaseUser> SignUpAsync(string email, string password)
    {
        var body = new Dictionary<string, object>
        {
            ["email"] = email,
            ["password"] = password,
            ["returnSecureToken"] = true
        };

        var json = await _client.PostAsync(AuthUrl("accounts:signUp"), body);
        return ParseUser(json);
    }

    public async Task<FirebaseUser> SignInWithPasswordAsync(string email, string password)
    {
        var body = new Dictionary<string, object>
        {
            ["email"] = email,
            ["password"] = password,
            ["returnSecureToken"] = true
        };

        var json = await _client.PostAsync(AuthUrl("accounts:signInWithPassword"), body);
        return ParseUser(json);
    }

    public async Task SendVerificationEmailAsync(string idToken)
    {
        var body = new Dictionary<string, object>
        {
            ["requestType"] = "VERIFY_EMAIL",
            ["idToken"] = idToken
        };

        await _client.PostAsync(AuthUrl("accounts:sendOobCode"), body);
    }

    public async Task SendPasswordResetEmailAsync(string email)
    {
        var body = new Dictionary<string, object>
        {
            ["requestType"] = "PASSWORD_RESET",
            ["email"] = email
        };

        await _client.PostAsync(AuthUrl("accounts:sendOobCode"), body);
    }

    public async Task ChangePasswordAsync(string idToken, string newPassword)
    {
        var body = new Dictionary<string, object>
        {
            ["idToken"] = idToken,
            ["password"] = newPassword,
            ["returnSecureToken"] = true
        };

        await _client.PostAsync(AuthUrl("accounts:update"), body);
    }

    public async Task DeleteAccountAsync(string idToken)
    {
        var body = new Dictionary<string, object>
        {
            ["idToken"] = idToken
        };

        await _client.PostAsync(AuthUrl("accounts:delete"), body);
    }

    public async Task<FirebaseUser> RefreshTokenAsync(string refreshToken)
    {
        var body = new Dictionary<string, object>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };

        var json = await _client.PostAsync(
            $"{FirebaseConfig.SecureTokenUrl}?key={FirebaseConfig.ApiKey}",
            body);

        var uid = GetUidFromIdToken(json.GetProperty("id_token").GetString() ?? "");

        return new FirebaseUser
        {
            IdToken = json.GetProperty("id_token").GetString() ?? "",
            RefreshToken = json.GetProperty("refresh_token").GetString() ?? "",
            Uid = uid,
            Email = "",
            EmailVerified = false,
            ExpiresInSeconds = json.GetProperty("expires_in").GetString() ?? "3600"
        };
    }

    public async Task<FirebaseUser> GetUserInfoAsync(string idToken)
    {
        var body = new Dictionary<string, object>
        {
            ["idToken"] = idToken
        };

        var json = await _client.PostAsync(AuthUrl("accounts:lookup"), body);
        var user = json.GetProperty("users")[0];

        return new FirebaseUser
        {
            IdToken = idToken,
            RefreshToken = "",
            Uid = user.TryGetProperty("localId", out var localId) ? localId.GetString() ?? "" : "",
            Email = user.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
            EmailVerified = user.TryGetProperty("emailVerified", out var verified) && verified.GetBoolean(),
            DisplayName = user.TryGetProperty("displayName", out var name) ? name.GetString() ?? "" : "",
            ExpiresInSeconds = "3600"
        };
    }

    private static string GetUidFromIdToken(string idToken)
    {
        var parts = idToken.Split('.');
        if (parts.Length < 2) return "";

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("user_id", out var uid)
                ? uid.GetString() ?? ""
                : "";
        }
        catch
        {
            return "";
        }
    }

    private static FirebaseUser ParseUser(JsonElement json)
    {
        var idToken = json.GetProperty("idToken").GetString() ?? "";
        return new FirebaseUser
        {
            IdToken = idToken,
            RefreshToken = json.GetProperty("refreshToken").GetString() ?? "",
            Uid = json.GetProperty("localId").GetString() ?? "",
            Email = json.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
            DisplayName = json.TryGetProperty("displayName", out var name) ? name.GetString() ?? "" : "",
            ExpiresInSeconds = json.GetProperty("expiresIn").GetString() ?? "3600"
        };
    }
}
