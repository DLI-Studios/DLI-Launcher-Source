using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.Firebase;

public class FirebaseApiException : Exception
{
    public string ErrorCode { get; }

    public FirebaseApiException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class FirebaseClient : IFirebaseClient
{
    public string? ApiKey => FirebaseConfig.ApiKey;
    public string? ProjectId => FirebaseConfig.ProjectId;

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<JsonElement> PostAsync(string url, object body)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await Http.PostAsync(url, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw ParseError(responseJson);
        }

        return JsonDocument.Parse(responseJson).RootElement.Clone();
    }

    public async Task<JsonElement> PostBytesAsync(string url, byte[] body, string contentType)
    {
        using var content = new ByteArrayContent(body);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        using var response = await Http.PostAsync(url, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw ParseError(responseJson);
        }

        return JsonDocument.Parse(responseJson).RootElement.Clone();
    }

    public async Task<JsonElement> GetAsync(string url)
    {
        using var response = await Http.GetAsync(url);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw ParseError(responseJson);
        }

        return JsonDocument.Parse(responseJson).RootElement.Clone();
    }

    public async Task<JsonElement> PatchAsync(string url, object body)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        using var response = await Http.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw ParseError(responseJson);
        }

        return JsonDocument.Parse(responseJson).RootElement.Clone();
    }

    public async Task DeleteAsync(string url)
    {
        using var response = await Http.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync();
            throw ParseError(responseJson);
        }
    }

    private static FirebaseApiException ParseError(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var error = doc.RootElement.GetProperty("error");
            var message = error.GetProperty("message").GetString() ?? "Unknown error";

            var code = "";

            // Firestore REST: {"error":{"code":404,"message":"...","status":"NOT_FOUND"}}
            if (error.TryGetProperty("status", out var status))
            {
                code = status.GetString() ?? "";
            }

            // Identity Toolkit: {"error":{"code":400,"message":"EMAIL_EXISTS: ..."}}
            if (string.IsNullOrEmpty(code) && error.TryGetProperty("code", out var httpCode) && httpCode.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                code = httpCode.GetInt32() switch
                {
                    404 => "NOT_FOUND",
                    401 or 403 => "PERMISSION_DENIED",
                    _ => ""
                };
            }

            if (string.IsNullOrEmpty(code))
            {
                var colon = message.IndexOf(':');
                code = colon > 0 ? message[..colon] : message;
                if (code.StartsWith('(')) code = code.TrimStart('(').TrimEnd(')');
            }

            if (string.IsNullOrEmpty(code)) code = "unknown";

            return new FirebaseApiException(code, message);
        }
        catch
        {
            return new FirebaseApiException("unknown", responseJson);
        }
    }
}
