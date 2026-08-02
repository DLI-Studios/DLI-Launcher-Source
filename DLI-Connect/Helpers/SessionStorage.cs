using System;
using System.IO;
using System.Text.Json;

namespace DLI.Connect.Helpers;

public static class SessionStorage
{
    private static readonly string BaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DLI Connect");

    private static string SessionFile => Path.Combine(BaseDir, SessionFileName);

    private static string SessionFileName
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] == "--profile" && i + 1 < args.Length)
                {
                    return $"session-{args[i + 1]}.json";
                }
                if (args[i].StartsWith("--profile="))
                {
                    return $"session-{args[i]["--profile=".Length..]}.json";
                }
            }
            return "session.json";
        }
    }

    public static void SaveRefreshToken(string refreshToken)
    {
        try
        {
            Directory.CreateDirectory(BaseDir);
            File.WriteAllText(SessionFile, JsonSerializer.Serialize(new { refreshToken }));
        }
        catch
        {
            // Best effort persistence
        }
    }

    public static string? LoadRefreshToken()
    {
        try
        {
            if (!File.Exists(SessionFile)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(SessionFile));
            return doc.RootElement.TryGetProperty("refreshToken", out var token)
                ? token.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(SessionFile)) File.Delete(SessionFile);
        }
        catch
        {
            // Best effort
        }
    }
}
