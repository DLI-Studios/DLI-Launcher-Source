using System;
using System.IO;
using DLI.Connect.Firebase;
using DLI.Connect.Utilities;

namespace DLI.Connect.Utilities;

public static class AppErrors
{
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "dli-connect.log");

    public static string ToMessage(Exception ex) =>
        ex is FirebaseApiException fbe
            ? Validators.ToTurkishErrorMessage(fbe.ErrorCode)
            : ex is System.Net.Http.HttpRequestException
                ? Validators.ToTurkishErrorMessage("NETWORK_ERROR")
                : ex.Message;

    public static void Log(string source, Exception ex)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex}\n\n");
        }
        catch { }
    }
}